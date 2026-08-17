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
    /// Chịu trách nhiệm phân loại và định nghĩa các đơn vị cơ sở cho từng nhóm (Ví dụ: Khối lượng, Thể tích).
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
        public async Task<IEnumerable<UoMCategoryReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.UoMCategories
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var categories = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UoMCategoryReadDto>>(categories);
        }

        /// <summary>
        /// Tìm kiếm nâng cao và phân trang danh sách nhóm đơn vị.
        /// </summary>
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
                var lowerSearch = search.Trim().ToLower();
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
        public async Task<int> CreateAsync(UoMCategoryCreateDto dto)
        {
            var trimmedCode = dto.Code.Trim();
            var trimmedName = dto.Name.Trim();

            if (await _context.UoMCategories.AnyAsync(c => c.Code == trimmedCode))
                throw new Exception($"Mã nhóm ĐVT '{trimmedCode}' đã tồn tại.");

            if (await _context.UoMCategories.AnyAsync(c => c.Name == trimmedName))
                throw new Exception($"Tên nhóm ĐVT '{trimmedName}' đã tồn tại.");

            var newCategory = _mapper.Map<UoMCategory>(dto);
            newCategory.Code = trimmedCode;
            newCategory.Name = trimmedName;
            newCategory.IsActive = dto.IsActive;
            newCategory.CreatedAt = DateTime.UtcNow;
            newCategory.UpdatedAt = DateTime.UtcNow;

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
                throw new KeyNotFoundException("Không tìm thấy nhóm ĐVT cần cập nhật.");

            var trimmedCode = dto.Code.Trim();
            var trimmedName = dto.Name.Trim();

            if (await _context.UoMCategories.AnyAsync(c => c.Id != id && c.Code == trimmedCode))
                throw new Exception($"Cập nhật thất bại: Mã nhóm ĐVT '{trimmedCode}' đã bị trùng lặp.");

            if (await _context.UoMCategories.AnyAsync(c => c.Id != id && c.Name == trimmedName))
                throw new Exception($"Cập nhật thất bại: Tên nhóm ĐVT '{trimmedName}' đã bị trùng lặp.");

            _mapper.Map(dto, category);
            category.Code = trimmedCode;
            category.Name = trimmedName;
            category.IsActive = dto.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Xóa danh mục nhóm đơn vị (có kiểm tra đơn vị tính con trực thuộc).
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.UoMCategories.FindAsync(id);
            if (category == null) throw new KeyNotFoundException("Không tìm thấy nhóm ĐVT để xóa.");

            // DATA INTEGRITY SHIELD: Kiểm tra xem có ĐVT con nào thuộc nhóm này không
            var hasUoMs = await _context.UoMs.AnyAsync(u => u.CategoryId == id && !u.IsDeleted);
            if (hasUoMs)
                throw new Exception("Không thể xóa nhóm này vì đang có các Đơn vị tính trực thuộc.");

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
            if (category == null) throw new KeyNotFoundException("Không tìm thấy nhóm ĐVT.");

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion
    }
}