using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service cung cấp dữ liệu Cửa hàng (Shop B2C/B2B Front-end).
    /// </summary>
    public class ShopProductService : IShopProductService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ShopProductService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        /// <inheritdoc />
        public async Task<PagedResult<ShopProductCardDto>> GetProductsAsync(ShopProductFilterParams filter)
        {
            var now = DateTime.UtcNow;

            // 1. Query cơ bản: Sản phẩm đang Active, không xóa mềm
            var query = _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c!.CategoryGroup)
                .Include(p => p.BaseUoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                        .ThenInclude(pr => pr.UoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Attributes)
                        .ThenInclude(a => a.AttributeDefinition)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.PromotionVariants)
                        .ThenInclude(pv => pv.PromotionCampaign)
                .Where(p => p.IsActive && !p.IsDeleted)
                .AsQueryable();

            // 2. Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string kw = filter.Search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(kw) ||
                                         p.Code.ToLower().Contains(kw) ||
                                         (p.Slug != null && p.Slug.ToLower().Contains(kw)) ||
                                         p.Variants.Any(v => v.Name.ToLower().Contains(kw) || v.Code.ToLower().Contains(kw)));
            }

            // 3. Lọc theo Nhóm Ngành Hàng (Group Slug)
            if (!string.IsNullOrWhiteSpace(filter.CategoryGroupSlug))
            {
                string groupSlug = filter.CategoryGroupSlug.Trim().ToLower();
                query = query.Where(p => p.Category != null &&
                                         p.Category.CategoryGroup != null &&
                                         ((p.Category.CategoryGroup.Slug != null && p.Category.CategoryGroup.Slug.ToLower() == groupSlug) ||
                                          p.Category.CategoryGroup.Id.ToString() == groupSlug));
            }

            // 4. Lọc theo Danh Mục (Category Slug)
            if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
            {
                string catSlug = filter.CategorySlug.Trim().ToLower();
                query = query.Where(p => p.Category != null &&
                                         ((p.Category.Slug != null && p.Category.Slug.ToLower() == catSlug) ||
                                          p.Category.Id.ToString() == catSlug));
            }

            // 5. Lọc theo Xuất xứ / Vùng trồng
            if (!string.IsNullOrWhiteSpace(filter.Origin))
            {
                string originVal = filter.Origin.Trim().ToLower();
                query = query.Where(p => p.Variants.Any(v => v.Attributes.Any(a =>
                    a.AttributeDefinition != null &&
                    a.AttributeDefinition.Name.ToLower().Contains("xuất xứ") &&
                    a.AttributeValue.ToLower().Contains(originVal))));
            }

            // 6. Lọc theo Tiêu chuẩn / Chứng nhận
            if (!string.IsNullOrWhiteSpace(filter.Certification))
            {
                string certVal = filter.Certification.Trim().ToLower();
                query = query.Where(p => p.Variants.Any(v => v.Attributes.Any(a =>
                    a.AttributeDefinition != null &&
                    (a.AttributeDefinition.Name.ToLower().Contains("chứng nhận") || a.AttributeDefinition.Name.ToLower().Contains("tiêu chuẩn")) &&
                    a.AttributeValue.ToLower().Contains(certVal))));
            }

            // Lấy danh sách ID Variant để query tồn kho khả dụng
            var productList = await query.ToListAsync();

            var variantIds = productList.SelectMany(p => p.Variants.Select(v => v.Id)).Distinct().ToList();

            // Truy vấn tồn kho khả dụng từ các Kho Bán Lẻ theo lô còn hạn sử dụng
            var baseInventoriesQuery = _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => variantIds.Contains(wi.VariantId) &&
                             wi.QuantityAvailable > 0 &&
                             (wi.Batch == null || wi.Batch.ExpiryDate > now));

            var filteredInventoriesQuery = await baseInventoriesQuery.FilterRetailOnlyAsync(_context);

            var inventories = await filteredInventoriesQuery
                .GroupBy(wi => wi.VariantId)
                .Select(g => new { VariantId = g.Key, TotalAvailable = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.VariantId, x => x.TotalAvailable);

            // Chuyển đổi sang Card DTO và tính giá
            var cardList = new List<ShopProductCardDto>();

            foreach (var p in productList)
            {
                var activeVariants = p.Variants.Where(v => v.IsActive && !v.IsDeleted).ToList();
                if (!activeVariants.Any())
                    continue;

                decimal productTotalStock = activeVariants.Sum(v => inventories.TryGetValue(v.Id, out decimal qty) ? qty : 0);

                // Lấy tất cả giá bán của các biến thể
                var allPrices = activeVariants
                    .SelectMany(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                    .ToList();

                if (!allPrices.Any())
                    continue;

                decimal minPrice = allPrices.Min(pr => pr.Price);
                decimal maxPrice = allPrices.Max(pr => pr.Price);

                // Chọn biến thể đại diện (default hoặc giá thấp nhất)
                var defaultPrice = allPrices.FirstOrDefault(pr => pr.IsDefault) ?? allPrices.OrderBy(pr => pr.Price).First();
                decimal originalPrice = defaultPrice.Price;
                decimal discountedPrice = originalPrice;
                decimal discountPercent = 0;
                bool hasPromotion = false;
                string? promoName = null;

                // Kiểm tra khuyến mãi đang áp dụng cho biến thể
                var parentVariant = activeVariants.FirstOrDefault(v => v.Id == defaultPrice.VariantId);
                if (parentVariant != null)
                {
                    var activePromo = parentVariant.PromotionVariants
                        .Select(pv => pv.PromotionCampaign)
                        .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                        .OrderByDescending(pc => pc!.DiscountValue)
                        .FirstOrDefault();

                    if (activePromo != null)
                    {
                        hasPromotion = true;
                        promoName = activePromo.Name;
                        if (activePromo.IsPercentage)
                        {
                            discountPercent = activePromo.DiscountValue;
                            discountedPrice = Math.Round(originalPrice * (1 - discountPercent / 100m));
                        }
                        else
                        {
                            discountedPrice = Math.Max(0, originalPrice - activePromo.DiscountValue);
                            discountPercent = originalPrice > 0 ? Math.Round((originalPrice - discountedPrice) / originalPrice * 100m) : 0;
                        }
                    }
                }

                // Trích xuất EAV
                string? origin = null;
                string? certification = null;
                string? brix = null;

                var allAttrs = activeVariants
                    .SelectMany(v => v.Attributes.Where(a => a.AttributeDefinition != null))
                    .ToList();

                var originAttr = allAttrs.FirstOrDefault(a => a.AttributeDefinition!.Name.ToLower().Contains("xuất xứ") || a.AttributeDefinition!.Name.ToLower().Contains("vùng trồng"));
                if (originAttr != null) origin = originAttr.AttributeValue;

                var certAttr = allAttrs.FirstOrDefault(a => a.AttributeDefinition!.Name.ToLower().Contains("chứng nhận") || a.AttributeDefinition!.Name.ToLower().Contains("tiêu chuẩn"));
                if (certAttr != null) certification = certAttr.AttributeValue;

                var brixAttr = allAttrs.FirstOrDefault(a => a.AttributeDefinition!.Name.ToLower().Contains("brix") || a.AttributeDefinition!.Name.ToLower().Contains("độ ngọt"));
                if (brixAttr != null) brix = brixAttr.AttributeValue;

                cardList.Add(new ShopProductCardDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Slug = !string.IsNullOrEmpty(p.Slug) ? p.Slug : SlugHelper.GenerateSlug(p.Name),
                    ImagePath = p.ImagePath ?? activeVariants.FirstOrDefault(v => !string.IsNullOrEmpty(v.ImagePath))?.ImagePath,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name,
                    CategorySlug = !string.IsNullOrEmpty(p.Category?.Slug) ? p.Category.Slug : (p.Category != null ? SlugHelper.GenerateSlug(p.Category.Name) : null),
                    CategoryGroupName = p.Category?.CategoryGroup?.Name,
                    CategoryGroupSlug = !string.IsNullOrEmpty(p.Category?.CategoryGroup?.Slug) ? p.Category.CategoryGroup.Slug : (p.Category?.CategoryGroup != null ? SlugHelper.GenerateSlug(p.Category.CategoryGroup.Name) : null),
                    BaseUoMName = p.BaseUoM?.Name ?? "Kg",
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    OriginalPrice = originalPrice,
                    DiscountedPrice = discountedPrice,
                    DiscountPercent = discountPercent,
                    HasPromotion = hasPromotion,
                    PromotionName = promoName,
                    Origin = origin,
                    Certification = certification,
                    BrixLevel = brix,
                    IsInStock = productTotalStock > 0,
                    TotalAvailableStock = productTotalStock
                });
            }

            // 7. Lọc theo khoảng giá
            if (filter.MinPrice.HasValue)
                cardList = cardList.Where(c => c.DiscountedPrice >= filter.MinPrice.Value).ToList();
            if (filter.MaxPrice.HasValue)
                cardList = cardList.Where(c => c.DiscountedPrice <= filter.MaxPrice.Value).ToList();

            // 8. Sắp xếp (Sort)
            cardList = filter.SortBy switch
            {
                "price-asc" => cardList.OrderBy(c => c.DiscountedPrice).ToList(),
                "price-desc" => cardList.OrderByDescending(c => c.DiscountedPrice).ToList(),
                "name-asc" => cardList.OrderBy(c => c.Name).ToList(),
                "discount" => cardList.OrderByDescending(c => c.DiscountPercent).ToList(),
                _ => cardList.OrderByDescending(c => c.Id).ToList() // Mới nhất mặc định
            };

            // 9. Phân trang
            int totalRecords = cardList.Count;
            int pageIndex = filter.PageIndex <= 0 ? 1 : filter.PageIndex;
            int pageSize = filter.PageSize <= 0 ? 12 : filter.PageSize;
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var pagedItems = cardList.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult<ShopProductCardDto>
            {
                Items = pagedItems,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<ShopProductDetailDto?> GetProductBySlugAsync(string slug)
        {
            var now = DateTime.UtcNow;

            var product = await _context.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c!.CategoryGroup)
                .Include(p => p.BaseUoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                        .ThenInclude(pr => pr.UoM)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Attributes)
                        .ThenInclude(a => a.AttributeDefinition)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.PromotionVariants)
                        .ThenInclude(pv => pv.PromotionCampaign)
                .FirstOrDefaultAsync(p => (p.Slug == slug || p.Id.ToString() == slug) && p.IsActive && !p.IsDeleted);

            if (product == null)
            {
                var allProds = await _context.Products
                    .Include(p => p.Category)
                        .ThenInclude(c => c!.CategoryGroup)
                    .Include(p => p.BaseUoM)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Prices.Where(pr => pr.IsActive && !pr.IsDeleted))
                            .ThenInclude(pr => pr.UoM)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.Attributes)
                            .ThenInclude(a => a.AttributeDefinition)
                    .Include(p => p.Variants)
                        .ThenInclude(v => v.PromotionVariants)
                            .ThenInclude(pv => pv.PromotionCampaign)
                    .Where(p => p.IsActive && !p.IsDeleted)
                    .ToListAsync();

                product = allProds.FirstOrDefault(p => SlugHelper.GenerateSlug(p.Name) == slug);
            }

            if (product == null)
                return null;

            var activeVariants = product.Variants.Where(v => v.IsActive && !v.IsDeleted).ToList();
            var variantIds = activeVariants.Select(v => v.Id).ToList();

            // Lấy danh sách quy đổi đơn vị tính đặc thù của sản phẩm hoặc toàn hệ thống
            var conversions = await _context.UoMConversions
                .Include(c => c.FromUoM)
                .Include(c => c.ToUoM)
                .Where(c => (c.ProductId == product.Id || c.ProductId == null) && c.IsActive && !c.IsDeleted)
                .ToListAsync();

            // Lấy tồn kho khả dụng cho các biến thể từ Kho Bán Lẻ
            var baseSlugInventoriesQuery = _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => variantIds.Contains(wi.VariantId) &&
                             wi.QuantityAvailable > 0 &&
                             (wi.Batch == null || wi.Batch.ExpiryDate > now));

            var filteredSlugInventoriesQuery = await baseSlugInventoriesQuery.FilterRetailOnlyAsync(_context);

            var inventories = await filteredSlugInventoriesQuery
                .GroupBy(wi => wi.VariantId)
                .Select(g => new { VariantId = g.Key, TotalAvailable = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.VariantId, x => x.TotalAvailable);

            // Gom thuộc tính EAV cấp sản phẩm
            var productAttrs = new Dictionary<string, string>();
            foreach (var v in activeVariants)
            {
                foreach (var attr in v.Attributes.Where(a => a.AttributeDefinition != null))
                {
                    if (!productAttrs.ContainsKey(attr.AttributeDefinition!.Name))
                    {
                        productAttrs[attr.AttributeDefinition.Name] = attr.AttributeValue;
                    }
                }
            }

            // Danh sách khuyến mãi đang áp dụng (Ánh xạ qua AutoMapper)
            var activePromoEntities = activeVariants
                .SelectMany(v => v.PromotionVariants)
                .Select(pv => pv.PromotionCampaign)
                .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                .GroupBy(pc => pc!.Id)
                .Select(g => g.First()!)
                .ToList();

            var activePromos = _mapper.Map<List<ShopPromotionBadgeDto>>(activePromoEntities);

            // Map biến thể
            var variantDtos = new List<ShopProductVariantDto>();
            foreach (var v in activeVariants)
            {
                decimal stock = inventories.TryGetValue(v.Id, out decimal qty) ? qty : 0;

                // Tìm khuyến mãi tốt nhất của biến thể
                var promo = v.PromotionVariants
                    .Select(pv => pv.PromotionCampaign)
                    .Where(pc => pc != null && pc.IsActive && !pc.IsDeleted && pc.StartDate <= now && pc.EndDate >= now)
                    .OrderByDescending(pc => pc!.DiscountValue)
                    .FirstOrDefault();

                var prices = v.Prices.Select(pr =>
                {
                    decimal discPrice = pr.Price;
                    decimal discPercent = 0;
                    if (promo != null)
                    {
                        if (promo.IsPercentage)
                        {
                            discPercent = promo.DiscountValue;
                            discPrice = Math.Round(pr.Price * (1 - discPercent / 100m));
                        }
                        else
                        {
                            discPrice = Math.Max(0, pr.Price - promo.DiscountValue);
                            discPercent = pr.Price > 0 ? Math.Round((pr.Price - discPrice) / pr.Price * 100m) : 0;
                        }
                    }

                    // Tìm tỷ lệ quy đổi từ ĐVT này (pr.UoMId) sang ĐVT cơ sở (product.BaseUoMId)
                    decimal factor = 1;
                    string? convText = null;

                    if (pr.UoMId == product.BaseUoMId)
                    {
                        factor = 1;
                        convText = null;
                    }
                    else
                    {
                        // Ưu tiên quy đổi đặc thù theo sản phẩm trước, sau đó tới quy đổi toàn cục
                        var conv = conversions.FirstOrDefault(c => c.ProductId == product.Id && c.FromUoMId == pr.UoMId)
                                ?? conversions.FirstOrDefault(c => c.ProductId == null && c.FromUoMId == pr.UoMId);

                        if (conv != null && conv.ConversionFactor > 0)
                        {
                            factor = conv.ConversionFactor;
                            string targetUom = conv.ToUoM?.Name ?? product.BaseUoM?.Name ?? "Kg";
                            convText = $"1 {pr.UoM?.Name} = {conv.ConversionFactor:#,##0.##} {targetUom}";
                        }
                    }

                    return new ShopVariantPriceDto
                    {
                        PriceId = pr.Id,
                        UoMId = pr.UoMId,
                        UoMName = pr.UoM?.Name ?? string.Empty,
                        Price = pr.Price,
                        DiscountedPrice = discPrice,
                        DiscountPercent = discPercent,
                        IsDefault = pr.IsDefault,
                        ConversionFactor = factor,
                        ConversionText = convText
                    };
                }).ToList();

                var varAttrs = v.Attributes
                    .Where(a => a.AttributeDefinition != null)
                    .ToDictionary(a => a.AttributeDefinition!.Name, a => a.AttributeValue);

                variantDtos.Add(new ShopProductVariantDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    Name = v.Name,
                    Description = v.Description,
                    ImagePath = v.ImagePath ?? product.ImagePath,
                    Prices = prices,
                    Attributes = varAttrs,
                    QuantityAvailable = stock,
                    IsInStock = stock > 0
                });
            }

            return new ShopProductDetailDto
            {
                Id = product.Id,
                Code = product.Code,
                Name = product.Name,
                Slug = !string.IsNullOrEmpty(product.Slug) ? product.Slug : SlugHelper.GenerateSlug(product.Name),
                Description = product.Description,
                ImagePath = product.ImagePath,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                CategorySlug = !string.IsNullOrEmpty(product.Category?.Slug) ? product.Category.Slug : (product.Category != null ? SlugHelper.GenerateSlug(product.Category.Name) : null),
                CategoryGroupName = product.Category?.CategoryGroup?.Name,
                CategoryGroupSlug = !string.IsNullOrEmpty(product.Category?.CategoryGroup?.Slug) ? product.Category.CategoryGroup.Slug : (product.Category?.CategoryGroup != null ? SlugHelper.GenerateSlug(product.Category.CategoryGroup.Name) : null),
                BaseUoMId = product.BaseUoMId,
                BaseUoMName = product.BaseUoM?.Name ?? "Kg",
                Attributes = productAttrs,
                Variants = variantDtos,
                ActivePromotions = activePromos
            };
        }

        /// <inheritdoc />
        public async Task<List<ShopCategoryTreeDto>> GetCategoryTreeAsync()
        {
            var groups = await _context.ProductCategoryGroups
                .Include(g => g.Categories.Where(c => c.IsActive && !c.IsDeleted))
                    .ThenInclude(c => c.Products.Where(p => p.IsActive && !p.IsDeleted))
                .Where(g => g.IsActive && !g.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<ShopCategoryTreeDto>>(groups);
        }

        /// <inheritdoc />
        public async Task<List<ShopProductCardDto>> GetFeaturedProductsAsync(int limit = 8)
        {
            var result = await GetProductsAsync(new ShopProductFilterParams
            {
                PageSize = limit,
                SortBy = "discount"
            });
            return result.Items.Take(limit).ToList();
        }

        /// <inheritdoc />
        public async Task<List<ShopProductCardDto>> GetNewArrivalsAsync(int limit = 8)
        {
            var result = await GetProductsAsync(new ShopProductFilterParams
            {
                PageSize = limit,
                SortBy = "newest"
            });
            return result.Items.Take(limit).ToList();
        }

        /// <inheritdoc />
        public async Task<List<ShopPromotionBadgeDto>> GetActivePromotionsAsync()
        {
            var now = DateTime.UtcNow;
            var promos = await _context.PromotionCampaigns
                .Where(p => p.IsActive && !p.IsDeleted && p.StartDate <= now && p.EndDate >= now)
                .OrderByDescending(p => p.DiscountValue)
                .ToListAsync();

            return _mapper.Map<List<ShopPromotionBadgeDto>>(promos);
        }

        /// <inheritdoc />
        public async Task<ShopPromotionDetailDto?> GetPromotionBySlugAsync(string slug)
        {
            var now = DateTime.UtcNow;
            var promo = await _context.PromotionCampaigns
                .Include(p => p.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Product)
                .FirstOrDefaultAsync(p => (p.Slug == slug || p.Id.ToString() == slug) && p.IsActive && !p.IsDeleted);

            if (promo == null)
                return null;

            var productIds = promo.PromotionVariants
                .Where(pv => pv.Variant != null && pv.Variant.Product != null)
                .Select(pv => pv.Variant!.ProductId)
                .Distinct()
                .ToList();

            var allCards = await GetProductsAsync(new ShopProductFilterParams { PageSize = 100 });
            var promoProducts = allCards.Items.Where(item => productIds.Contains(item.Id)).ToList();

            var detailDto = _mapper.Map<ShopPromotionDetailDto>(promo);
            detailDto.Products = promoProducts;

            return detailDto;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetAvailableOriginsAsync()
        {
            var origins = await _context.ProductAttributes
                .Include(pa => pa.AttributeDefinition)
                .Where(pa => pa.AttributeDefinition != null &&
                             (pa.AttributeDefinition.Name.ToLower().Contains("xuất xứ") || pa.AttributeDefinition.Name.ToLower().Contains("vùng trồng")) &&
                             !string.IsNullOrEmpty(pa.AttributeValue))
                .Select(pa => pa.AttributeValue.Trim())
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return origins;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetAvailableCertificationsAsync()
        {
            var certs = await _context.ProductAttributes
                .Include(pa => pa.AttributeDefinition)
                .Where(pa => pa.AttributeDefinition != null &&
                             (pa.AttributeDefinition.Name.ToLower().Contains("chứng nhận") || pa.AttributeDefinition.Name.ToLower().Contains("tiêu chuẩn")) &&
                             !string.IsNullOrEmpty(pa.AttributeValue))
                .Select(pa => pa.AttributeValue.Trim())
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return certs;
        }
    }
}
