using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductBatchDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Lô Hàng Nông Sản (Product Batches / Lots).
    /// Quản lý thông tin ngày sản xuất, hạn sử dụng, nhà cung cấp và biến thể sản phẩm theo từng lô nhập.
    /// </summary>
    public class ProductBatchService : IProductBatchService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductBatchService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Read Operations

        /// <inheritdoc />
        public async Task<IEnumerable<ProductBatchReadDto>> GetAllListAsync()
        {
            var items = await _context.ProductBatches
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductBatchReadDto>>(items);
        }

        /// <inheritdoc />
        public async Task<PagedResult<ProductBatchReadDto>> GetPagedAsync(
            string? search,
            int? variantId,
            int? supplierId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ProductBatches
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsQueryable();

            // 1. Filter: Tìm kiếm theo Mã lô
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.BatchCode.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Theo Biến thể sản phẩm
            if (variantId.HasValue && variantId.Value > 0)
                query = query.Where(x => x.VariantId == variantId.Value);

            // 3. Filter: Theo Nhà cung cấp
            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            // 4. Filter: Trạng thái
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // 5. Filter: Ngày tạo
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

            var dtos = _mapper.Map<IEnumerable<ProductBatchReadDto>>(items);

            return new PagedResult<ProductBatchReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<ProductBatchReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.ProductBatches
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy lô hàng.");

            return _mapper.Map<ProductBatchReadDto>(entity);
        }

        #endregion

        #region Write Operations

        /// <inheritdoc />
        public async Task<int> CreateAsync(ProductBatchCreateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var trimmedCode = dto.BatchCode.Trim();

                // 1. Business Rule: Validate NSX & HSD
                if (dto.ExpiryDate <= dto.ManufactureDate)
                    throw new InvalidOperationException("Hạn sử dụng phải lớn hơn Ngày sản xuất.");

                // 2. Bắt lỗi trùng Mã Lô (không phân biệt hoa thường)
                if (await _context.ProductBatches.AnyAsync(x => x.BatchCode.ToLower() == trimmedCode.ToLower()))
                    throw new InvalidOperationException($"Mã lô hàng '{trimmedCode}' đã tồn tại trong hệ thống.");

                // 3. Kiểm tra tính hợp lệ của VariantId
                if (!await _context.ProductVariants.AnyAsync(v => v.Id == dto.VariantId && !v.IsDeleted))
                    throw new InvalidOperationException("Biến thể sản phẩm không tồn tại hoặc đã bị xóa.");

                // 4. Kiểm tra tính hợp lệ của SupplierId nếu có
                if (dto.SupplierId > 0 && !await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId && !s.IsDeleted))
                    throw new InvalidOperationException("Nhà cung cấp không tồn tại hoặc đã bị xóa.");

                var entity = _mapper.Map<ProductBatch>(dto);
                entity.BatchCode = trimmedCode;
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.ProductBatches.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(int id, ProductBatchUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.ProductBatches.FindAsync(id);
                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy lô hàng cần sửa.");

                // 1. Business Rule: Validate NSX & HSD
                if (dto.ExpiryDate <= dto.ManufactureDate)
                    throw new InvalidOperationException("Hạn sử dụng phải lớn hơn Ngày sản xuất.");

                // 2. Map các trường được phép cập nhật (ManufactureDate, ExpiryDate, IsActive)
                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.ProductBatches.FindAsync(id);
                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy lô hàng để xóa.");

                // 1. SAFETY SHIELD: Chặn xóa nếu lô hàng đang có tồn kho khả dụng
                var hasInventory = await _context.WarehouseInventories.AnyAsync(wi => wi.BatchId == id && wi.QuantityAvailable > 0);
                if (hasInventory)
                    throw new InvalidOperationException("Không thể xóa lô hàng này vì đang có tồn kho khả dụng trong kho.");

                // 2. SAFETY SHIELD: Chặn xóa nếu đã phát sinh trong phiếu nhập kho
                var hasReceipt = await _context.InventoryReceiptDetails.AnyAsync(ird => ird.BatchId == id);
                if (hasReceipt)
                    throw new InvalidOperationException("Không thể xóa lô hàng này vì đã phát sinh trong lịch sử phiếu nhập kho.");

                // 3. SAFETY SHIELD: Chặn xóa nếu đã phát sinh trong phiếu xuất kho
                var hasIssues = await _context.InventoryIssueDetails.AnyAsync(iid => iid.BatchId == id);
                if (hasIssues)
                    throw new InvalidOperationException("Không thể xóa lô hàng này vì đã phát sinh trong lịch sử phiếu xuất kho.");

                _context.ProductBatches.Remove(entity);
                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <inheritdoc />
        public async Task<bool> ToggleActiveAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.ProductBatches.FindAsync(id);
                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy lô hàng.");

                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            });
        }

        #endregion
    }
}