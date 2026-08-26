using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;
using backend.Helpers;
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

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm đơn vị tính (không phân trang).
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
                .AsNoTracking()
                .AsQueryable();

            #region Bộ lọc tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

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
            #endregion

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UoMCategoryReadDto>
            {
                Items = _mapper.Map<IEnumerable<UoMCategoryReadDto>>(items),
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

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới danh mục nhóm đơn vị.
        /// </summary>
        public async Task<int> CreateAsync(UoMCategoryCreateDto dto)
        {
            var normalizedCode = dto.Code.Trim().ToUpper();
            if (await _context.UoMCategories.AnyAsync(c => c.Code.ToUpper() == normalizedCode))
                throw new InvalidOperationException($"Mã nhóm ĐVT '{dto.Code.Trim()}' đã tồn tại trong hệ thống.");

            var trimmedName = dto.Name.Trim();
            if (await _context.UoMCategories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower()))
                throw new InvalidOperationException($"Tên nhóm ĐVT '{dto.Name.Trim()}' đã tồn tại trong hệ thống.");

            if (dto.BaseUoMId.HasValue && !await _context.UoMs.AnyAsync(u => u.Id == dto.BaseUoMId.Value && !u.IsDeleted))
                throw new InvalidOperationException("Đơn vị tính cơ sở không tồn tại hoặc đã bị xóa.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var newCategory = _mapper.Map<UoMCategory>(dto);

                _context.UoMCategories.Add(newCategory);
                await _context.SaveChangesAsync();
                return newCategory.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin danh mục nhóm đơn vị.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, UoMCategoryUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var category = await _context.UoMCategories.FirstOrDefaultAsync(c => c.Id == id);
                if (category == null)
                    throw new KeyNotFoundException($"Không tìm thấy nhóm ĐVT với ID = {id}.");

                var normalizedCode = dto.Code.Trim().ToUpper();
                if (await _context.UoMCategories.AnyAsync(c => c.Id != id && c.Code.ToUpper() == normalizedCode))
                    throw new InvalidOperationException($"Cập nhật thất bại: Mã nhóm ĐVT '{dto.Code.Trim()}' đã bị trùng lặp.");

                var trimmedName = dto.Name.Trim();
                if (await _context.UoMCategories.AnyAsync(c => c.Id != id && c.Name.ToLower() == trimmedName.ToLower()))
                    throw new InvalidOperationException($"Cập nhật thất bại: Tên nhóm ĐVT '{dto.Name.Trim()}' đã bị trùng lặp.");

                if (dto.BaseUoMId.HasValue && !await _context.UoMs.AnyAsync(u => u.Id == dto.BaseUoMId.Value && !u.IsDeleted))
                    throw new InvalidOperationException("Đơn vị tính cơ sở không tồn tại hoặc đã bị xóa.");

                _mapper.Map(dto, category);
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa danh mục nhóm đơn vị (có kiểm tra đơn vị tính con trực thuộc).
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var category = await _context.UoMCategories.FindAsync(id);
                if (category == null)
                    throw new KeyNotFoundException($"Không tìm thấy nhóm ĐVT với ID = {id}.");

                // DATA INTEGRITY SHIELD: Kiểm tra xem có ĐVT con nào thuộc nhóm này không
                var hasUoMs = await _context.UoMs.AnyAsync(u => u.CategoryId == id && !u.IsDeleted);
                if (hasUoMs)
                    throw new InvalidOperationException("Không thể xóa nhóm này vì đang có các Đơn vị tính trực thuộc.");

                _context.UoMCategories.Remove(category);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Đảo ngược trạng thái hoạt động của nhóm đơn vị.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var category = await _context.UoMCategories.FindAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Không tìm thấy nhóm ĐVT với ID = {id}.");

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return category.IsActive;
        }

        #endregion
    }
}