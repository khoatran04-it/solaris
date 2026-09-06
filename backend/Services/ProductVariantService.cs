using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductVariantDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Biến Thể Sản Phẩm (SKU - Stock Keeping Unit).
    /// Chịu trách nhiệm xử lý nghiệp vụ cho các đơn vị hàng hóa vật lý cụ thể, bao gồm việc thiết lập Giá bán theo ĐVT và các Thuộc tính động (EAV).
    /// </summary>
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
        #region Read Operations

        /// <inheritdoc />
        public async Task<IEnumerable<ProductVariantReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.Prices).ThenInclude(p => p.UoM)
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductVariantReadDto>>(items);

            // Áp dụng khuyến mãi lên từng dòng giá của biến thể
            ApplyPromotionsToDtos(items, dtos);

            // Tính tồn kho khả dụng thực tế
            await PopulateAvailableStockAsync(dtos);

            return dtos;
        }

        /// <inheritdoc />
        public async Task<PagedResult<ProductVariantReadDto>> GetPagedAsync(
            string? search, string? productId, bool? isActive, DateTime? createdAt, DateTime? updatedAt, int pageIndex, int pageSize)
        {
            var query = _context.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.Attributes)
                    .ThenInclude(a => a.AttributeDefinition)
                .Include(x => x.Prices).ThenInclude(p => p.UoM)
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
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

            // Áp dụng khuyến mãi lên từng dòng giá
            ApplyPromotionsToDtos(items, dtos);

            // Tính tồn kho khả dụng thực tế
            await PopulateAvailableStockAsync(dtos);

            return new PagedResult<ProductVariantReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<ProductVariantReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.ProductVariants
                .Include(x => x.Product)
                .Include(x => x.Attributes)
                    .ThenInclude(a => a.AttributeDefinition)
                .Include(x => x.Prices).ThenInclude(p => p.UoM)
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.PromotionCampaign)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm.");

            var dto = _mapper.Map<ProductVariantReadDto>(entity);
            var dtoList = new List<ProductVariantReadDto> { dto };

            // Áp dụng khuyến mãi
            ApplyPromotionsToDtos(new List<ProductVariant> { entity }, dtoList);

            // Tính tồn kho khả dụng thực tế
            await PopulateAvailableStockAsync(dtoList);

            return dto;
        }

        #endregion

        // ==========================================================
        // 2. CÁC HÀM TẠO VÀ CẬP NHẬT (XỬ LÝ TRANSACTION)
        // ==========================================================
        #region Write Operations

        /// <inheritdoc />
        public async Task<int> CreateAsync(ProductVariantCreateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var trimmedCode = dto.Code.Trim();

                if (await _context.ProductVariants.IgnoreQueryFilters().AnyAsync(x => x.Code.ToLower() == trimmedCode.ToLower()))
                    throw new InvalidOperationException($"Mã SKU '{trimmedCode}' của biến thể này đã tồn tại trong hệ thống.");

                // Kiểm tra ProductId hợp lệ
                if (!await _context.Products.AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted))
                    throw new InvalidOperationException("Sản phẩm gốc không tồn tại hoặc đã bị xóa.");

                var entity = _mapper.Map<ProductVariant>(dto);
                entity.Code = trimmedCode;
                entity.Name = dto.Name.Trim();
                entity.Description = dto.Description?.Trim();
                entity.ImagePath = dto.ImagePath?.Trim();
                if (dto.LengthCm.HasValue && dto.WidthCm.HasValue && dto.HeightCm.HasValue && dto.LengthCm.Value > 0 && dto.WidthCm.Value > 0 && dto.HeightCm.Value > 0)
                {
                    if (!dto.UnitCbm.HasValue || dto.UnitCbm.Value <= 0)
                    {
                        entity.UnitCbm = Math.Round((dto.LengthCm.Value * dto.WidthCm.Value * dto.HeightCm.Value) / 1000000m, 4);
                    }
                }
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsActive = dto.IsActive;

                _context.ProductVariants.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(int id, ProductVariantUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.ProductVariants
                    .Include(x => x.Attributes)
                    .Include(x => x.Prices)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm cần sửa.");

                var trimmedCode = dto.Code.Trim();
                if (await _context.ProductVariants.IgnoreQueryFilters().AnyAsync(x => x.Id != id && x.Code.ToLower() == trimmedCode.ToLower()))
                    throw new InvalidOperationException($"Cập nhật thất bại: Mã SKU '{trimmedCode}' này đã bị trùng với một biến thể khác.");

                // Kiểm tra ProductId hợp lệ
                if (!await _context.Products.AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted))
                    throw new InvalidOperationException("Sản phẩm gốc không tồn tại hoặc đã bị xóa.");

                // 1. Map các trường cơ bản (AutoMapper đã Ignore Attributes và Prices)
                _mapper.Map(dto, entity);
                entity.Code = trimmedCode;
                entity.Name = dto.Name.Trim();
                entity.Description = dto.Description?.Trim();
                entity.ImagePath = dto.ImagePath?.Trim();
                if (entity.LengthCm.HasValue && entity.WidthCm.HasValue && entity.HeightCm.HasValue && entity.LengthCm.Value > 0 && entity.WidthCm.Value > 0 && entity.HeightCm.Value > 0)
                {
                    if (!dto.UnitCbm.HasValue || dto.UnitCbm.Value <= 0)
                    {
                        entity.UnitCbm = Math.Round((entity.LengthCm.Value * entity.WidthCm.Value * entity.HeightCm.Value) / 1000000m, 4);
                    }
                }
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsActive = dto.IsActive;

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
                        attr.CreatedAt = DateTime.UtcNow;
                        attr.UpdatedAt = DateTime.UtcNow;
                        attr.IsActive = true;
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
                        price.CreatedAt = DateTime.UtcNow;
                        price.UpdatedAt = DateTime.UtcNow;
                        price.IsActive = true;
                        _context.Add(price);
                    }
                }

                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.ProductVariants.FindAsync(id);
                if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể để xóa.");

                // 1. SAFETY SHIELD: Chặn xóa nếu có Lô hàng
                var hasBatches = await _context.ProductBatches.AnyAsync(b => b.VariantId == id && !b.IsDeleted);
                if (hasBatches)
                    throw new InvalidOperationException("Không thể xóa biến thể này vì đang có các lô hàng liên kết.");

                // 2. SAFETY SHIELD: Chặn xóa nếu có Bảng giá Nhà cung cấp
                var hasSupplierProducts = await _context.SupplierProducts.AnyAsync(sp => sp.VariantId == id && !sp.IsDeleted);
                if (hasSupplierProducts)
                    throw new InvalidOperationException("Không thể xóa biến thể này vì đang có bảng giá nhà cung cấp liên kết.");

                // 3. SAFETY SHIELD: Chặn xóa nếu có trong Đơn mua hàng (PO)
                var hasPO = await _context.PurchaseOrderDetails.AnyAsync(pod => pod.VariantId == id);
                if (hasPO)
                    throw new InvalidOperationException("Không thể xóa biến thể này vì đã phát sinh trong đơn mua hàng (PO).");

                // 4. SAFETY SHIELD: Chặn xóa nếu có trong Đơn bán hàng (SO)
                var hasSO = await _context.OrderDetails.AnyAsync(od => od.VariantId == id);
                if (hasSO)
                    throw new InvalidOperationException("Không thể xóa biến thể này vì đã phát sinh trong đơn bán hàng.");

                // 5. SAFETY SHIELD: Chặn xóa nếu có trong Phiếu nhập kho
                var hasIR = await _context.InventoryReceiptDetails.AnyAsync(ird => ird.VariantId == id);
                if (hasIR)
                    throw new InvalidOperationException("Không thể xóa biến thể này vì đã phát sinh trong phiếu nhập kho.");

                // 6. SAFETY SHIELD: Chặn xóa nếu có trong Phiếu xuất kho
                var hasII = await _context.InventoryIssueDetails.AnyAsync(iid => iid.VariantId == id);
                if (hasII)
                    throw new InvalidOperationException("Không thể xóa biến thể này vì đã phát sinh trong phiếu xuất kho.");

                _context.ProductVariants.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> ToggleActiveAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.ProductVariants.FindAsync(id);
                if (entity == null) throw new KeyNotFoundException("Không tìm thấy biến thể sản phẩm.");

                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion

        // ==========================================================
        // 3. PRIVATE HELPERS
        // ==========================================================
        #region Private Helpers

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

                var activeCampaigns = variant.PromotionVariants?
                    .Where(pv => pv.PromotionCampaign != null
                              && pv.PromotionCampaign.IsActive
                              && pv.PromotionCampaign.StartDate <= now
                              && pv.PromotionCampaign.EndDate >= now)
                    .Select(pv => pv.PromotionCampaign!)
                    .ToList() ?? new List<PromotionCampaign>();

                if (!activeCampaigns.Any() || !dto.Prices.Any()) continue;

                foreach (var priceDto in dto.Prices)
                {
                    decimal bestPrice = priceDto.Price;
                    bool isDiscounted = false;

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

        /// <summary>
        /// Tính toán tổng tồn kho khả dụng từ két sắt tồn kho 4 ngăn (WarehouseInventories),
        /// chỉ tính các lô hàng còn hạn sử dụng (FEFO) và gán vào DTO biến thể.
        /// </summary>
        private async Task PopulateAvailableStockAsync(List<ProductVariantReadDto> dtos)
        {
            if (dtos == null || dtos.Count == 0) return;

            var variantIds = dtos.Select(x => x.Id).ToList();
            var now = DateTime.UtcNow;

            var stockDict = await _context.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Batch)
                .Where(wi => variantIds.Contains(wi.VariantId) &&
                             wi.QuantityAvailable > 0 &&
                             (wi.Batch == null || wi.Batch.ExpiryDate > now))
                .GroupBy(wi => wi.VariantId)
                .Select(g => new { VariantId = g.Key, TotalAvailable = g.Sum(x => x.QuantityAvailable) })
                .ToDictionaryAsync(x => x.VariantId, x => x.TotalAvailable);

            foreach (var dto in dtos)
            {
                dto.QuantityAvailable = stockDict.TryGetValue(dto.Id, out var s) ? s : 0;
            }
        }

        #endregion
    }
}