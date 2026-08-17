using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerTypeDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Phân loại khách hàng (Customer Types).
    /// Đảm bảo tính duy nhất của mã phân loại, tính toàn vẹn dữ liệu và hỗ trợ cơ chế Soft Delete.
    /// </summary>
    public class CustomerTypeService : ICustomerTypeService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerTypeService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách phân loại khách hàng, sắp xếp theo tên.
        /// </summary>
        public async Task<IEnumerable<CustomerTypeReadDto>> GetAllListAsync()
        {
            var types = await _context.CustomerTypes
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerTypeReadDto>>(types);
        }

        /// <summary>
        /// Tìm kiếm nâng cao và phân trang danh sách phân loại khách hàng.
        /// </summary>
        public async Task<PagedResult<CustomerTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.CustomerTypes.AsQueryable();

            // 1. Tìm kiếm theo từ khóa (Mã hoặc Tên)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(lowerSearch) || x.Name.ToLower().Contains(lowerSearch));
            }

            // 2. Lọc theo danh sách tên khách hàng (phân tách bằng dấu phẩy)
            if (!string.IsNullOrWhiteSpace(names))
            {
                var nameList = names.Split(',').Select(n => n.Trim().ToLower()).ToList();
                query = query.Where(x => nameList.Contains(x.Name.ToLower()));
            }

            // 3. Lọc theo trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 4. Lọc theo ngày tạo
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 5. Lọc theo ngày cập nhật
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

            var dtos = _mapper.Map<IEnumerable<CustomerTypeReadDto>>(items);

            return new PagedResult<CustomerTypeReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết thông tin phân loại khách hàng theo ID.
        /// </summary>
        public async Task<CustomerTypeReadDto?> GetByIdAsync(int id)
        {
            var type = await _context.CustomerTypes.FindAsync(id);
            if (type == null) return null;

            return _mapper.Map<CustomerTypeReadDto>(type);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới phân loại khách hàng. Kiểm tra ràng buộc duy nhất cho Code.
        /// </summary>
        public async Task<int> CreateAsync(CustomerTypeCreateDto dto)
        {
            if (await _context.CustomerTypes.AnyAsync(x => x.Code == dto.Code.Trim()))
                throw new Exception($"Mã phân loại '{dto.Code}' đã tồn tại trên hệ thống.");

            var entity = _mapper.Map<CustomerType>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsActive = dto.IsActive;

            _context.CustomerTypes.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin phân loại. Đảm bảo mã Code không trùng với các bản ghi khác.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, CustomerTypeUpdateDto dto)
        {
            var entity = await _context.CustomerTypes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại cần sửa.");

            if (await _context.CustomerTypes.AnyAsync(x => x.Id != id && x.Code == dto.Code.Trim()))
                throw new Exception($"Cập nhật thất bại: Mã phân loại '{dto.Code}' đã được sử dụng bởi phân loại khác.");

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa phân loại khách hàng. Kiểm tra ràng buộc không có khách hàng liên kết.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CustomerTypes
                .Include(x => x.Customers)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại cần xóa.");

            if (entity.Customers.Any(c => !c.IsDeleted))
                throw new Exception("Không thể xóa phân loại này vì đang có Khách hàng liên kết.");

            _context.CustomerTypes.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm khóa của phân loại khách hàng.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.CustomerTypes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại khách hàng.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}