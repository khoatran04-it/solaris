using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerGroupDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Nhóm khách hàng (Customer Groups).
    /// Xử lý các nghiệp vụ về phân loại nhóm, trạng thái hoạt động và đảm bảo tính nhất quán dữ liệu.
    /// </summary>
    public class CustomerGroupService : ICustomerGroupService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerGroupService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm khách hàng để hiển thị trong các bộ chọn (Dropdown/Select).
        /// </summary>
        public async Task<IEnumerable<CustomerGroupReadDto>> GetAllListAsync()
        {
            var groups = await _context.CustomerGroups
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerGroupReadDto>>(groups);
        }

        /// <summary>
        /// Truy vấn danh sách nhóm khách hàng có phân trang, lọc theo trạng thái và thời gian.
        /// </summary>
        public async Task<PagedResult<CustomerGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.CustomerGroups.AsQueryable();

            // 1. Filter: Tìm kiếm đa cột (Mã hoặc Tên)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch)
                );
            }

            // 2. Filter: Lọc theo danh sách tên nhóm cụ thể
            if (!string.IsNullOrWhiteSpace(names))
            {
                var nameList = names.Split(',').Select(n => n.Trim().ToLower()).ToList();
                query = query.Where(x => nameList.Contains(x.Name.ToLower()));
            }

            // 3. Filter: Lọc theo trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 4. Filter: Theo ngày (Xử lý so sánh trọn ngày 24h)
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

            // Thực hiện phân trang và tối ưu hóa bộ nhớ với AsNoTracking
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<CustomerGroupReadDto>>(items);

            return new PagedResult<CustomerGroupReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết thông tin một nhóm khách hàng theo ID.
        /// </summary>
        public async Task<CustomerGroupReadDto?> GetByIdAsync(int id)
        {
            var group = await _context.CustomerGroups.FindAsync(id);
            if (group == null) return null;

            return _mapper.Map<CustomerGroupReadDto>(group);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới nhóm khách hàng. Ràng buộc mã Code không được trùng lặp.
        /// </summary>
        public async Task<int> CreateAsync(CustomerGroupCreateDto dto)
        {
            if (await _context.CustomerGroups.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã nhóm khách hàng đã tồn tại trên hệ thống.");

            var entity = _mapper.Map<CustomerGroup>(dto);

            _context.CustomerGroups.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin nhóm khách hàng. Kiểm tra tính duy nhất của mã Code mới.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, CustomerGroupUpdateDto dto)
        {
            var entity = await _context.CustomerGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm khách hàng cần sửa.");

            // Đảm bảo mã nhóm không trùng lặp với các bản ghi khác trong Database
            if (await _context.CustomerGroups.AnyAsync(x => x.Id != id && x.Code == dto.Code))
                throw new Exception("Cập nhật thất bại: Mã nhóm này đã bị trùng lặp với dữ liệu khác.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa nhóm khách hàng. 
        /// Lưu ý: Sử dụng Soft Delete để đảm bảo không mất dữ liệu lịch sử của khách hàng trong nhóm.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CustomerGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm khách hàng cần xóa.");

            // Thao tác xóa vật lý sẽ được Interceptor tự động chuyển đổi thành xóa mềm (IsDeleted = true)
            _context.CustomerGroups.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Thay đổi trạng thái Hoạt động/Khóa của nhóm khách hàng một cách nhanh chóng.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.CustomerGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm khách hàng.");

            // Đảo ngược trạng thái hiện tại
            entity.IsActive = !entity.IsActive;

            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}