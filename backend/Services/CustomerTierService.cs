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
        // ==========================================
        // SECTION: FIELDS & CONSTRUCTOR
        // ==========================================
        #region Fields & Constructor
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CustomerTierService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #endregion

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
        /// Lấy danh sách bậc hạng có phân trang và tìm kiếm.
        /// </summary>
        public async Task<PagedResult<CustomerTierReadDto>> GetPagedAsync(
            string? search,
            string? names,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.CustomerTiers.AsQueryable();

            // Filter: Tìm kiếm theo mã hoặc tên hạng
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(lowerSearch) || x.Name.ToLower().Contains(lowerSearch));
            }

            // Filter: Lọc theo thời gian tạo (Quét trọn ngày)
            if (createdAt.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= createdAt.Value.Date && x.CreatedAt < createdAt.Value.Date.AddDays(1));
            }

            if (updatedAt.HasValue)
            {
                query = query.Where(x => x.UpdatedAt >= updatedAt.Value.Date && x.UpdatedAt < updatedAt.Value.Date.AddDays(1));
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
            // Business Rule: Mỗi bậc hạng phải có một mã duy nhất để định danh
            if (await _context.CustomerTiers.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã bậc hạng đã tồn tại trên hệ thống.");

            var entity = _mapper.Map<CustomerTier>(dto);

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

            // Business Rule: Đảm bảo mã Code mới không bị trùng với các bậc hạng khác
            if (await _context.CustomerTiers.AnyAsync(x => x.Id != id && x.Code == dto.Code))
                throw new Exception("Cập nhật thất bại: Mã bậc hạng này đã bị trùng lặp với dữ liệu khác.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa bậc hạng khách hàng.
        /// </summary>
        /// <remarks>Thao tác xóa vật lý sẽ được Interceptor chuyển thành Soft Delete (Xóa mềm).</remarks>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CustomerTiers.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy bậc hạng cần xóa.");

            // Note: Việc kiểm tra ràng buộc sử dụng (isUsed) đã được thay thế bằng cơ chế 
            // Soft Delete để giữ lại lịch sử cho các khách hàng đã thuộc hạng này.
            _context.CustomerTiers.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}