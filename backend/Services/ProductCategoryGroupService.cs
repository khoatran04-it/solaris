using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductCategoryGroupDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Nhóm Ngành Hàng (Product Category Groups).
    /// </summary>
    public class ProductCategoryGroupService : IProductCategoryGroupService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductCategoryGroupService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm loại sản phẩm (không phân trang)
        /// </summary>
        public async Task<IEnumerable<ProductCategoryGroupReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.ProductCategoryGroups.AsNoTracking().AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var types = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductCategoryGroupReadDto>>(types);
        }

        /// <summary>
        /// Lấy danh sách phân loại nhóm sản phẩm có phân trang và tìm kiếm
        /// </summary>
        public async Task<PagedResult<ProductCategoryGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ProductCategoryGroups.AsQueryable();

            // 1. Filter: Tìm kiếm theo mã hoặc tên
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch)
                );
            }

            // 2. Filter: Tìm kiếm theo danh sách tên cụ thể
            if (!string.IsNullOrWhiteSpace(names))
            {
                var nameList = names.Split(',').Select(n => n.Trim().ToLower()).ToList();
                query = query.Where(x => nameList.Contains(x.Name.ToLower()));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 3. Filter: Theo ngày tạo
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
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<ProductCategoryGroupReadDto>>(items);

            return new PagedResult<ProductCategoryGroupReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết phân loại theo ID
        /// </summary>
        public async Task<ProductCategoryGroupReadDto?> GetByIdAsync(int id)
        {
            var type = await _context.ProductCategoryGroups.FindAsync(id);
            if (type == null) return null;

            return _mapper.Map<ProductCategoryGroupReadDto>(type);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới nhóm loại sản phẩm
        /// </summary>
        public async Task<int> CreateAsync(ProductCategoryGroupCreateDto dto)
        {
            var trimmedCode = dto.Code.Trim();

            if (await _context.ProductCategoryGroups.AnyAsync(x => x.Code == trimmedCode))
                throw new Exception($"Mã nhóm ngành hàng '{trimmedCode}' đã tồn tại.");

            var entity = _mapper.Map<ProductCategoryGroup>(dto);
            entity.Code = trimmedCode;
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.ImagePath = dto.ImagePath?.Trim();
            entity.IsActive = dto.IsActive;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.ProductCategoryGroups.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin nhóm loại sản phẩm
        /// </summary>
        public async Task<bool> UpdateAsync(int id, ProductCategoryGroupUpdateDto dto)
        {
            var entity = await _context.ProductCategoryGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm ngành hàng cần sửa.");

            var trimmedCode = dto.Code.Trim();

            if (await _context.ProductCategoryGroups.AnyAsync(x => x.Id != id && x.Code == trimmedCode))
                throw new Exception($"Cập nhật thất bại: Mã nhóm ngành hàng '{trimmedCode}' đã bị trùng lặp.");

            _mapper.Map(dto, entity);
            entity.Code = trimmedCode;
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.ImagePath = dto.ImagePath?.Trim();
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa phân nhóm loại sản phẩm (Có kiểm tra danh mục con)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductCategoryGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm ngành hàng cần xóa.");

            // Kiểm tra ràng buộc toàn vẹn dữ liệu
            bool hasCategories = await _context.ProductCategories.AnyAsync(c => c.CategoryGroupId == id && !c.IsDeleted);
            if (hasCategories)
                throw new Exception("Không thể xóa nhóm ngành hàng này vì đang chứa các danh mục hàng hóa con.");

            _context.ProductCategoryGroups.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Đổi trạng thái Hoạt động / Khóa của nhóm ngành hàng
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.ProductCategoryGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm ngành hàng.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}