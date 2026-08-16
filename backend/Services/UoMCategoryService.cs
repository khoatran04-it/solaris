using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Nhóm đơn vị tính (Unit of Measure Categories).
    /// Chịu trách nhiệm phân loại và định nghĩa các đơn vị cơ sở cho từng nhóm (Ví dụ: Khối lượng, Độ dài).
    /// </summary>
    public class UoMCategoryService : IUoMCategoryService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public UoMCategoryService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm đơn vị tính.
        /// </summary>
        /// <remarks>Sử dụng Include để nạp thông tin đơn vị gốc (BaseUoM) phục vụ hiển thị trên DTO.</remarks>
        public async Task<IEnumerable<UoMCategoryReadDto>> GetAllListAsync()
        {
            var categories = await _context.UoMCategories
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UoMCategoryReadDto>>(categories);
        }

        /// <summary>
        /// Tìm kiếm nâng cao và phân trang danh sách nhóm đơn vị.
        /// </summary>
        /// <remarks>
        /// Tối ưu: Lược bỏ AsSplitQuery vì quan hệ BaseUoM là 1-1, 
        /// giúp giảm số lượng câu lệnh SQL thực thi mà vẫn giữ được dữ liệu liên kết.
        /// </remarks>
        public async Task<PagedResult<UoMCategoryReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.UoMCategories
                .Include(x => x.BaseUoM)
                .AsQueryable();

            // 1. Filter: Tìm kiếm theo mã hoặc tên danh mục
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 3. Filter: Theo ngày tạo (So sánh trọn ngày 24h)
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 4. Filter: Theo ngày cập nhật
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

            var dtos = _mapper.Map<IEnumerable<UoMCategoryReadDto>>(items);

            return new PagedResult<UoMCategoryReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết một nhóm đơn vị kèm theo thông tin đơn vị gốc.
        /// </summary>
        public async Task<UoMCategoryReadDto?> GetByIdAsync(int id)
        {
            var category = await _context.UoMCategories
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null) return null;

            return _mapper.Map<UoMCategoryReadDto>(category);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới danh mục nhóm đơn vị.
        /// </summary>
        /// <exception cref="Exception">Ném ra khi Tên hoặc Mã danh mục đã tồn tại.</exception>
        public async Task<int> CreateAsync(UoMCategoryCreateDto dto)
        {
            // Business Rule: Đảm bảo tính duy nhất của Mã và Tên nhóm đơn vị
            if (await _context.UoMCategories.AnyAsync(c => c.Code == dto.Code || c.Name == dto.Name))
                throw new Exception("Tên hoặc mã danh mục đã tồn tại trên hệ thống.");

            var newCategory = _mapper.Map<UoMCategory>(dto);
            _context.UoMCategories.Add(newCategory);

            await _context.SaveChangesAsync();
            return newCategory.Id;
        }

        /// <summary>
        /// Cập nhật thông tin danh mục nhóm đơn vị.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, UoMCategoryUpdateDto dto)
        {
            var category = await _context.UoMCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
                throw new KeyNotFoundException("Không tìm thấy dữ liệu yêu cầu.");

            // Kiểm tra ràng buộc duy nhất (loại trừ bản ghi hiện tại)
            if (await _context.UoMCategories.AnyAsync(c => c.Id != id && (c.Code == dto.Code || c.Name == dto.Name)))
                throw new Exception("Cập nhật thất bại: Tên hoặc mã danh mục bị trùng lặp với dữ liệu khác.");

            _mapper.Map(dto, category);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Xóa danh mục nhóm đơn vị.
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.UoMCategories.FindAsync(id);
            if (category == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu cần xóa.");

            _context.UoMCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Đảo ngược trạng thái hoạt động của nhóm đơn vị.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var category = await _context.UoMCategories.FindAsync(id);
            if (category == null) throw new KeyNotFoundException("Không tìm thấy nhóm đơn vị.");

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }
}