using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.SupplierProductDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Bảng giá và Danh mục Sản phẩm cung cấp bởi Nhà cung cấp (Supplier Product).
    /// </summary>
    public class SupplierProductService : ISupplierProductService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public SupplierProductService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách liên kết sản phẩm - nhà cung cấp.
        /// </summary>
        public async Task<IEnumerable<SupplierProductReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseUoM)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierProductReadDto>>(items);
        }

        /// <summary>
        /// Lấy danh sách sản phẩm được cung cấp bởi một nhà cung cấp cụ thể.
        /// </summary>
        public async Task<IEnumerable<SupplierProductReadDto>> GetBySupplierIdAsync(int supplierId, bool isActiveOnly = true)
        {
            var query = _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseUoM)
                .Where(x => x.SupplierId == supplierId)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderBy(x => x.Variant != null ? x.Variant.Name : "")
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierProductReadDto>>(items);
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh mục sản phẩm nhà cung cấp.
        /// </summary>
        public async Task<PagedResult<SupplierProductReadDto>> GetPagedAsync(
            string? search,
            int? variantId,
            int? supplierId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseUoM)
                .AsQueryable();

            // 1. Filter: Tìm theo mã SKU của NCC, Tên biến thể hoặc Mã biến thể
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => 
                    (x.SupplierSKU != null && x.SupplierSKU.ToLower().Contains(lowerSearch)) ||
                    (x.Variant != null && (x.Variant.Name.ToLower().Contains(lowerSearch) || x.Variant.Code.ToLower().Contains(lowerSearch))) ||
                    (x.Supplier != null && x.Supplier.Name.ToLower().Contains(lowerSearch))
                );
            }

            // 2. Filter: Theo Biến thể
            if (variantId.HasValue && variantId.Value > 0)
                query = query.Where(x => x.VariantId == variantId.Value);

            // 3. Filter: Theo Nhà cung cấp
            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(x => x.SupplierId == supplierId.Value);

            // 4. Filter: Trạng thái
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // 5. Filter: Ngày tháng
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
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<SupplierProductReadDto>>(items);

            return new PagedResult<SupplierProductReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết liên kết sản phẩm - nhà cung cấp theo ID.
        /// </summary>
        public async Task<SupplierProductReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.SupplierProducts
                .Include(x => x.Variant)
                .Include(x => x.Supplier)
                .Include(x => x.PurchaseUoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin giá nhập của nhà cung cấp.");

            return _mapper.Map<SupplierProductReadDto>(entity);
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Tạo mới cấu hình giá nhập từ nhà cung cấp cho một biến thể sản phẩm.
        /// </summary>
        public async Task<int> CreateAsync(SupplierProductCreateDto dto)
        {
            // Kiểm tra tồn tại Biến thể sản phẩm
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == dto.VariantId);
            if (!variantExists)
                throw new InvalidOperationException("Biến thể sản phẩm được chọn không tồn tại.");

            // Kiểm tra tồn tại Nhà cung cấp
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId);
            if (!supplierExists)
                throw new InvalidOperationException("Nhà cung cấp được chọn không tồn tại.");

            // Kiểm tra tồn tại ĐVT mua hàng
            var uomExists = await _context.UoMs.AnyAsync(u => u.Id == dto.PurchaseUoMId);
            if (!uomExists)
                throw new InvalidOperationException("Đơn vị tính mua hàng được chọn không tồn tại.");

            // Business Rule: 1 Sản phẩm - 1 NCC chỉ được phép có 1 dòng cấu hình giá
            var isDuplicate = await _context.SupplierProducts
                .AnyAsync(x => x.VariantId == dto.VariantId && x.SupplierId == dto.SupplierId);

            if (isDuplicate)
                throw new InvalidOperationException("Nhà cung cấp này đã có cấu hình giá cho sản phẩm được chọn. Vui lòng cập nhật bản ghi hiện tại thay vì tạo mới.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<SupplierProduct>(dto);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.SupplierProducts.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật cấu hình giá nhập từ nhà cung cấp.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, SupplierProductUpdateDto dto)
        {
            var entity = await _context.SupplierProducts.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình giá nhập cần sửa.");

            // Kiểm tra tồn tại Biến thể sản phẩm
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == dto.VariantId);
            if (!variantExists)
                throw new InvalidOperationException("Biến thể sản phẩm được chọn không tồn tại.");

            // Kiểm tra tồn tại Nhà cung cấp
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == dto.SupplierId);
            if (!supplierExists)
                throw new InvalidOperationException("Nhà cung cấp được chọn không tồn tại.");

            // Kiểm tra tồn tại ĐVT mua hàng
            var uomExists = await _context.UoMs.AnyAsync(u => u.Id == dto.PurchaseUoMId);
            if (!uomExists)
                throw new InvalidOperationException("Đơn vị tính mua hàng được chọn không tồn tại.");

            // Bắt lỗi trùng lặp khi người dùng sửa Variant hoặc Supplier (ngoại trừ dòng hiện tại)
            var isDuplicate = await _context.SupplierProducts
                .AnyAsync(x => x.Id != id && x.VariantId == dto.VariantId && x.SupplierId == dto.SupplierId);

            if (isDuplicate)
                throw new InvalidOperationException("Cập nhật thất bại: Cấu hình liên kết giữa Sản phẩm và Nhà cung cấp này đã tồn tại.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa cấu hình giá nhập (Soft Delete).
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.SupplierProducts.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình giá nhập để xóa.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Bật / Tắt trạng thái hoạt động của bảng giá nhập.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.SupplierProducts.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình giá nhập.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}