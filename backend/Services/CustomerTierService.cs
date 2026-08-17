using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CustomerTierDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Phân hạng khách hàng (Loyalty Tiers).
    /// Hỗ trợ thiết lập các mức hạng dựa trên chi tiêu và các ưu đãi tương ứng.
    /// </summary>
    public class CustomerTierService : ICustomerTierService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerTierService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách bậc hạng.
        /// </summary>
        /// <remarks>Sắp xếp theo MinSpending để phục vụ logic so sánh thứ bậc trên giao diện.</remarks>
        public async Task<IEnumerable<CustomerTierReadDto>> GetAllListAsync()
        {
            var tiers = await _context.CustomerTiers
                .AsNoTracking()
                .OrderBy(x => x.MinSpending)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerTierReadDto>>(tiers);
        }

        /// <summary>
        /// Lấy danh sách bậc hạng có phân trang, tìm kiếm và lọc trạng thái.
        /// </summary>
        public async Task<PagedResult<CustomerTierReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.CustomerTiers.AsQueryable();

            // Filter: Tìm kiếm theo mã hoặc tên hạng
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(lowerSearch) || x.Name.ToLower().Contains(lowerSearch));
            }

            // Filter: Lọc theo danh sách tên
            if (!string.IsNullOrWhiteSpace(names))
            {
                var nameList = names.Split(',').Select(n => n.Trim().ToLower()).ToList();
                query = query.Where(x => nameList.Contains(x.Name.ToLower()));
            }

            // Filter: Trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // Filter: Lọc theo thời gian tạo (Quét trọn ngày)
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // Filter: Lọc theo thời gian cập nhật
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

            var dtos = _mapper.Map<IEnumerable<CustomerTierReadDto>>(items);

            return new PagedResult<CustomerTierReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết một bậc hạng theo ID.
        /// </summary>
        public async Task<CustomerTierReadDto?> GetByIdAsync(int id)
        {
            var tier = await _context.CustomerTiers.FindAsync(id);
            if (tier == null) return null;

            return _mapper.Map<CustomerTierReadDto>(tier);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới bậc hạng khách hàng. Kiểm tra trùng lặp mã Code.
        /// </summary>
        public async Task<int> CreateAsync(CustomerTierCreateDto dto)
        {
            if (await _context.CustomerTiers.AnyAsync(x => x.Code == dto.Code.Trim()))
                throw new Exception($"Mã bậc hạng '{dto.Code}' đã tồn tại trên hệ thống.");

            var entity = _mapper.Map<CustomerTier>(dto);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsActive = dto.IsActive;

            _context.CustomerTiers.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin bậc hạng.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, CustomerTierUpdateDto dto)
        {
            var entity = await _context.CustomerTiers.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy bậc hạng cần sửa.");

            if (await _context.CustomerTiers.AnyAsync(x => x.Id != id && x.Code == dto.Code.Trim()))
                throw new Exception($"Cập nhật thất bại: Mã bậc hạng '{dto.Code}' đã được sử dụng bởi bậc hạng khác.");

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa bậc hạng khách hàng. Kiểm tra ràng buộc an toàn.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CustomerTiers
                .Include(x => x.Customers)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy bậc hạng cần xóa.");

            if (entity.Customers.Any(c => !c.IsDeleted))
                throw new Exception("Không thể xóa bậc hạng này vì đang có Khách hàng thuộc hạng này.");

            _context.CustomerTiers.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm khóa của bậc hạng.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.CustomerTiers.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy bậc hạng khách hàng.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}