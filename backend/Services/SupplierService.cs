using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.SupplierDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý thông tin Nhà cung cấp.
    /// Xử lý các nghiệp vụ phức tạp về lọc dữ liệu, quản lý giao dịch (Transaction) 
    /// và đảm bảo tính toàn vẹn dữ liệu chuỗi cung ứng.
    /// </summary>
    public class SupplierService : ISupplierService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public SupplierService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách nhà cung cấp, ưu tiên các bản ghi mới nhất.
        /// </summary>
        public async Task<IEnumerable<SupplierReadDto>> GetAllListAsync()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.SupplierType)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierReadDto>>(suppliers);
        }

        /// <summary>
        /// Tìm kiếm nâng cao và phân trang danh sách nhà cung cấp.
        /// </summary>
        public async Task<PagedResult<SupplierReadDto>> GetPagedAsync(
            string? search,
            string? supplierTypesId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Suppliers
                .Include(s => s.SupplierType)
                .Include(s => s.Addresses)
                .AsQueryable();

            // 1. Filter: Tìm kiếm đa cột (Mã, Tên, Số điện thoại)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch) ||
                    x.Phone.Contains(lowerSearch)
                );
            }

            // 2. Filter: Lọc theo danh sách phân loại
            if (!string.IsNullOrWhiteSpace(supplierTypesId))
            {
                var typeIdList = supplierTypesId
                    .Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (typeIdList.Any())
                {
                    query = query.Where(x => x.SupplierTypeId.HasValue && typeIdList.Contains(x.SupplierTypeId.Value));
                }
            }

            // 3. Filter: Trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 4. Filter: Theo ngày tạo
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 5. Filter: Theo ngày cập nhật
            if (updatedAt.HasValue)
            {
                var startDate = updatedAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.UpdatedAt >= startDate && x.UpdatedAt < endDate);
            }

            var totalRecords = await query.CountAsync();

            // Thực hiện phân trang và tối ưu hóa truy vấn
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<SupplierReadDto>>(items);

            return new PagedResult<SupplierReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết nhà cung cấp bao gồm cả Phân loại và danh sách Địa chỉ.
        /// </summary>
        public async Task<SupplierReadDto?> GetByIdAsync(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.SupplierType)
                .Include(s => s.Addresses)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) return null;

            return _mapper.Map<SupplierReadDto>(supplier);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới nhà cung cấp kèm địa chỉ (nếu có). Sử dụng Transaction để đảm bảo tính toàn vẹn dữ liệu.
        /// </summary>
        public async Task<int> CreateAsync(SupplierCreateDto dto)
        {
            // Business Rule: Mã định danh và Số điện thoại không được trùng lặp
            if (await _context.Suppliers.AnyAsync(s => s.Code == dto.Code.Trim()))
                throw new Exception($"Mã nhà cung cấp '{dto.Code}' đã tồn tại trên hệ thống.");

            if (await _context.Suppliers.AnyAsync(s => s.Phone == dto.Phone.Trim()))
                throw new Exception($"Số điện thoại '{dto.Phone}' đã được đăng ký bởi nhà cung cấp khác.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newSupplier = _mapper.Map<Supplier>(dto);
                newSupplier.CreatedAt = DateTime.UtcNow;
                newSupplier.UpdatedAt = DateTime.UtcNow;
                newSupplier.IsActive = dto.IsActive;

                // Xử lý địa chỉ ban đầu nếu có
                if (newSupplier.Addresses != null && newSupplier.Addresses.Any())
                {
                    bool hasDefault = newSupplier.Addresses.Any(a => a.IsDefault);
                    if (!hasDefault)
                    {
                        newSupplier.Addresses.First().IsDefault = true;
                    }

                    foreach (var addr in newSupplier.Addresses)
                    {
                        addr.CreatedAt = DateTime.UtcNow;
                        addr.UpdatedAt = DateTime.UtcNow;
                    }
                }

                _context.Suppliers.Add(newSupplier);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return newSupplier.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp và kiểm tra ràng buộc duy nhất.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, SupplierUpdateDto dto)
        {
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
            if (supplier == null) throw new KeyNotFoundException("Không tìm thấy thông tin nhà cung cấp.");

            // Kiểm tra trùng lặp thông tin với các nhà cung cấp khác
            bool isCodeDuplicate = await _context.Suppliers.AnyAsync(s => s.Id != id && s.Code == dto.Code.Trim());
            if (isCodeDuplicate)
                throw new Exception($"Mã nhà cung cấp '{dto.Code}' đã được sử dụng bởi đơn vị khác.");

            bool isPhoneDuplicate = await _context.Suppliers.AnyAsync(s => s.Id != id && s.Phone == dto.Phone.Trim());
            if (isPhoneDuplicate)
                throw new Exception($"Số điện thoại '{dto.Phone}' đã được sử dụng bởi nhà cung cấp khác.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _mapper.Map(dto, supplier);
                supplier.UpdatedAt = DateTime.UtcNow;
                supplier.IsActive = dto.IsActive;

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

        /// <summary>
        /// Xóa nhà cung cấp. Kiểm tra ràng buộc toàn vẹn chuỗi cung ứng trước khi xóa.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Batches)
                .Include(s => s.PurchaseOrders)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) 
                throw new KeyNotFoundException("Không tìm thấy nhà cung cấp để xóa.");

            // Kiểm tra ràng buộc bảo vệ toàn vẹn dữ liệu
            if (supplier.PurchaseOrders.Any(p => !p.IsDeleted))
                throw new Exception("Không thể xóa nhà cung cấp này vì đã có Đơn mua hàng (PO) liên kết.");

            if (supplier.Batches.Any(b => !b.IsDeleted))
                throw new Exception("Không thể xóa nhà cung cấp này vì đã có Lô hàng (Batch) liên kết trong kho.");

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động (Kích hoạt/Khóa) của nhà cung cấp.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");

            supplier.IsActive = !supplier.IsActive;
            supplier.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}