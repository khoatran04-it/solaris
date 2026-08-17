using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Danh Mục Sản Phẩm (Product Categories).
    /// </summary>
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductCategoryService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách loại sản phẩm (không phân trang)
        /// </summary>
        public async Task<IEnumerable<ProductCategoryReadDto>> GetAllListAsync(bool isActiveOnly = false) 
        { 
            var query = _context.ProductCategories
                .Include(x => x.CategoryGroup)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var types = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductCategoryReadDto>>(types);
        }

        /// <summary>
        /// Lấy danh sách phân loại sản phẩm có phân trang và tìm kiếm
        /// </summary>
        public async Task<PagedResult<ProductCategoryReadDto>> GetPagedAsync(
            string? search,
            string? names,
            string? categoryGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ProductCategories
                .Include(x => x.CategoryGroup)
                .AsQueryable();

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

            // 3. Lọc Đa luồng: Nhóm Danh Mục Sản Phẩm (CategoryGroup)
            if (!string.IsNullOrWhiteSpace(categoryGroupId))
            {
                var groupIdList = categoryGroupId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (groupIdList.Any())
                {
                    query = query.Where(x => x.CategoryGroupId.HasValue && groupIdList.Contains(x.CategoryGroupId.Value));
                }
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 4. Filter: Ngày tạo
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 5. Filter: Ngày cập nhật
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

            var dtos = _mapper.Map<IEnumerable<ProductCategoryReadDto>>(items);

            return new PagedResult<ProductCategoryReadDto>
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
        public async Task<ProductCategoryReadDto?> GetByIdAsync(int id)
        {
            var type = await _context.ProductCategories
                .Include(x => x.CategoryGroup)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (type == null) return null;

            return _mapper.Map<ProductCategoryReadDto>(type);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới loại sản phẩm
        /// </summary>
        public async Task<int> CreateAsync(ProductCategoryCreateDto dto)
        {
            var trimmedCode = dto.Code.Trim();

            if (await _context.ProductCategories.AnyAsync(x => x.Code == trimmedCode))
                throw new Exception($"Mã danh mục '{trimmedCode}' đã tồn tại.");

            var entity = _mapper.Map<ProductCategory>(dto);
            entity.Code = trimmedCode;
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.ImagePath = dto.ImagePath?.Trim();
            entity.IsActive = dto.IsActive;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.ProductCategories.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin loại sản phẩm
        /// </summary>
        public async Task<bool> UpdateAsync(int id, ProductCategoryUpdateDto dto)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục sản phẩm cần sửa.");

            var trimmedCode = dto.Code.Trim();

            if (await _context.ProductCategories.AnyAsync(x => x.Id != id && x.Code == trimmedCode))
                throw new Exception($"Cập nhật thất bại: Mã danh mục '{trimmedCode}' đã bị trùng lặp.");

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
        /// Xóa loại sản phẩm (Có kiểm tra sản phẩm trực thuộc)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục để xóa.");

            // Kiểm tra ràng buộc toàn vẹn dữ liệu
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
            if (hasProducts)
                throw new Exception("Không thể xóa danh mục này vì đang có sản phẩm thuộc danh mục.");

            _context.ProductCategories.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Khóa của danh mục
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục sản phẩm.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
