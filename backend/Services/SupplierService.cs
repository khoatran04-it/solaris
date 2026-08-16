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
    /// và đảm bảo tính duy nhất của định danh nhà cung cấp.
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
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierReadDto>>(suppliers);
        }

        /// <summary>
        /// Tìm kiếm nâng cao và phân trang danh sách nhà cung cấp.
        /// </summary>
        /// <remarks>
        /// Sử dụng AsSplitQuery() để tối ưu hiệu năng khi Include nhiều bảng liên quan.
        /// </remarks>
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
                .AsQueryable();

            // 1. Filter: Tìm kiếm đa cột (Mã, Tên, Số điện thoại)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch) ||
                    x.Phone.Contains(lowerSearch)
                );
            }

            // 2. Filter: Lọc theo danh sách phân loại
            if (!string.IsNullOrWhiteSpace(supplierTypesId))
            {
                // Cắt chuỗi bằng dấu phẩy, an toàn check TryParse để tránh sập server nếu có lỗi chuỗi
                var typeIdList = supplierTypesId
                    .Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                // So sánh trực tiếp bằng khóa ngoại
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
                .AsSplitQuery() // Tránh Cartesian Explosion khi kết hợp nhiều tập dữ liệu
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
        /// Tạo mới nhà cung cấp. Sử dụng Transaction để đảm bảo tính toàn vẹn dữ liệu.
        /// </summary>
        public async Task<int> CreateAsync(SupplierCreateDto dto)
        {
            // Business Rule: Mã định danh và Số điện thoại không được trùng lặp
            if (await _context.Suppliers.AnyAsync(s => s.Code == dto.Code || s.Phone == dto.Phone))
                throw new Exception("Mã nhà cung cấp hoặc Số điện thoại đã tồn tại trên hệ thống.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newSupplier = _mapper.Map<Supplier>(dto);

                _context.Suppliers.Add(newSupplier);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return newSupplier.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new Exception("Quá trình lưu dữ liệu thất bại. Hệ thống đã hoàn tác các thay đổi.");
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
            bool isDuplicate = await _context.Suppliers.AnyAsync(s =>
                s.Id != id && (s.Code == dto.Code || s.Phone == dto.Phone));

            if (isDuplicate)
                throw new Exception("Cập nhật thất bại: Mã hoặc Số điện thoại đã được sử dụng bởi nhà cung cấp khác.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _mapper.Map(dto, supplier);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new Exception("Cập nhật thất bại. Hệ thống đã hoàn tác để bảo vệ dữ liệu.");
            }
        }

        /// <summary>
        /// Xóa nhà cung cấp. 
        /// Lưu ý: Thao tác này có thể được xử lý bởi Interceptor để thực hiện Soft Delete.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) 
                throw new KeyNotFoundException("Không tìm thấy nhà cung cấp để xóa.");

            // Thao tác Remove sẽ được Global Interceptor chuyển thành Update DeletedAt nếu dùng Soft Delete
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
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}