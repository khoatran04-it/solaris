using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductVariantDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ProductVariantService : IProductVariantService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductVariantService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================================
        // 1. CÁC HÀM GET & XỬ LÝ LOGIC KHUYẾN MÃI
        // ==========================================================

        public async Task<IEnumerable<ProductVariantReadDto>> GetAllListAsync()
        {
            var items = await _context.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.Prices).ThenInclude(p => p.UoM) // 🔥 Kéo theo bảng giá và đơn vị tính
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductVariantReadDto>>(items);

            // 🔥 Áp dụng khuyến mãi lên từng dòng giá của biến thể
            ApplyPromotionsToDtos(items, dtos);

            return dtos;
        }

        public async Task<PagedResult<ProductVariantReadDto>> GetPagedAsync(
            string? search, string? productId, bool? isActive, DateTime? createdAt, DateTime? updatedAt, int pageIndex, int pageSize)
        {
            var query = _context.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.Attributes)
                    .ThenInclude(a => a.AttributeDefinition)
                .Include(x => x.Prices).ThenInclude(p => p.UoM) // 🔥 Kéo theo bảng giá
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(lowerSearch) || x.Name.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(productId))
            {
                var productIdList = productId.Split(',').Where(idStr => int.TryParse(idStr.Trim(), out _)).Select(idStr => int.Parse(idStr.Trim())).ToList();
                if (productIdList.Any()) query = query.Where(x => productIdList.Contains(x.ProductId));
            }

            if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            if (updatedAt.HasValue)
            {
                var startDate = updatedAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.UpdatedAt >= startDate && x.UpdatedAt < endDate);
            }

            var totalRecords = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductVariantReadDto>>(items);

            // 🔥 Áp dụng khuyến mãi lên từng dòng giá
            ApplyPromotionsToDtos(items, dtos);

            return new PagedResult<ProductVariantReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<ProductVariantReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.Attributes)
                    .ThenInclude(a => a.AttributeDefinition)
                .Include(x => x.Prices).ThenInclude(p => p.UoM) // 🔥 Kéo theo bảng giá
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm.");

            var dto = _mapper.Map<ProductVariantReadDto>(entity);

            // 🔥 Áp dụng khuyến mãi
            ApplyPromotionsToDtos(new List<ProductVariant> { entity }, new List<ProductVariantReadDto> { dto });

            return dto;
        }

        // ==========================================================
        // 2. CÁC HÀM TẠO VÀ CẬP NHẬT (XỬ LÝ TRANSACTION)
        // ==========================================================

        public async Task<int> CreateAsync(ProductVariantCreateDto dto)
        {
            if (await _context.ProductVariants.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã SKU của biến thể này đã tồn tại trong hệ thống.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // AutoMapper sẽ tự động map mảng Attributes và Prices từ DTO sang Model
                var entity = _mapper.Map<ProductVariant>(dto);

                _context.ProductVariants.Add(entity);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return entity.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(int id, ProductVariantUpdateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 🔥 Kéo theo Attributes và Prices để RemoveRange
                var entity = await _context.ProductVariants
                    .Include(x => x.Attributes)
                    .Include(x => x.Prices)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm cần sửa.");

                if (await _context.ProductVariants.AnyAsync(x => x.Id != id && x.Code == dto.Code))
                    throw new Exception("Cập nhật thất bại: Mã SKU này đã bị trùng với một biến thể khác.");

                // 1. Map các trường cơ bản (AutoMapper đã Ignore Attributes và Prices)
                _mapper.Map(dto, entity);

                // 2. XÓA SẠCH thuộc tính và bảng giá cũ
                if (entity.Attributes.Any())
                {
                    _context.RemoveRange(entity.Attributes);
                }
                if (entity.Prices.Any())
                {
                    _context.RemoveRange(entity.Prices);
                }

                // 3. THÊM MỚI thuộc tính
                if (dto.Attributes != null && dto.Attributes.Any())
                {
                    var newAttributes = _mapper.Map<List<ProductAttribute>>(dto.Attributes);
                    foreach (var attr in newAttributes)
                    {
                        attr.VariantId = id;
                        _context.Add(attr);
                    }
                }

                // 4. THÊM MỚI Bảng giá
                if (dto.Prices != null && dto.Prices.Any())
                {
                    var newPrices = _mapper.Map<List<ProductVariantPrice>>(dto.Prices);
                    foreach (var price in newPrices)
                    {
                        price.VariantId = id;
                        _context.Add(price);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==========================================================
        // 3. CÁC HÀM XÓA & STATE
        // ==========================================================

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductVariants.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể để xóa.");

            _context.ProductVariants.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.ProductVariants.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm.");

            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================================
        // 4. PRIVATE HELPERS
        // ==========================================================

        /// <summary>
        /// Duyệt qua từng Quy cách bán (Prices) của biến thể, tính toán xem giá trị 
        /// khuyến mãi sâu nhất là bao nhiêu rồi gán vào DTO.
        /// </summary>
        private void ApplyPromotionsToDtos(List<ProductVariant> entities, List<ProductVariantReadDto> dtos)
        {
            var now = DateTime.UtcNow;

            for (int i = 0; i < entities.Count; i++)
            {
                var variant = entities[i];
                var dto = dtos[i];

                // Lọc ra các chiến dịch đang Active
                var activeCampaigns = variant.PromotionVariants?
                    .Where(pv => pv.PromotionCampaign != null
                              && pv.PromotionCampaign.IsActive
                              && pv.PromotionCampaign.StartDate <= now
                              && pv.PromotionCampaign.EndDate >= now)
                    .Select(pv => pv.PromotionCampaign!)
                    .ToList() ?? new List<PromotionCampaign>();

                if (!activeCampaigns.Any() || !dto.Prices.Any()) continue;

                // Duyệt qua từng đơn giá (Kg, Nải, Thùng...)
                foreach (var priceDto in dto.Prices)
                {
                    decimal bestPrice = priceDto.Price;
                    bool isDiscounted = false;

                    // Tính xem campaign nào giảm giá sâu nhất cho quy cách này
                    foreach (var campaign in activeCampaigns)
                    {
                        decimal currentCalcPrice = priceDto.Price;

                        if (campaign.IsPercentage)
                        {
                            currentCalcPrice = priceDto.Price * (1 - (campaign.DiscountValue / 100m));
                        }
                        else
                        {
                            currentCalcPrice = priceDto.Price - campaign.DiscountValue;
                        }

                        if (currentCalcPrice < 0) currentCalcPrice = 0;

                        if (currentCalcPrice < bestPrice)
                        {
                            bestPrice = currentCalcPrice;
                            isDiscounted = true;
                        }
                    }

                    if (isDiscounted)
                    {
                        priceDto.PromotionalPrice = bestPrice;
                    }
                }
            }
        }
    }
}