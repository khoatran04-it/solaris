using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Danh Mục Sản Phẩm (Product Categories).
    /// Đảm bảo tính toàn vẹn dữ liệu, kiểm tra tính duy nhất (case-insensitive) và hỗ trợ giao dịch kiên cường (Execution Strategy).
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

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách danh mục sản phẩm (không phân trang, thường dùng cho giao diện Dropdown/Select).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các danh mục đang ở trạng thái hoạt động.</param>
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

            var categories = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductCategoryReadDto>>(categories);
        }

        /// <summary>
        /// Lấy danh sách danh mục sản phẩm có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu đa chiều.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã hoặc tên danh mục.</param>
        /// <param name="names">Lọc theo một danh sách tên cụ thể (phân cách bằng dấu phẩy).</param>
        /// <param name="categoryGroupId">Chuỗi danh sách ID nhóm ngành hàng lớn (phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
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
                var nameList = names.Split(',')
                    .Select(n => n.Trim().ToLower())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (nameList.Any())
                {
                    query = query.Where(x => nameList.Contains(x.Name.ToLower()));
                }
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
                .OrderByDescending(x => x.CreatedAt)
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
        /// Lấy thông tin chi tiết của một danh mục sản phẩm theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của danh mục.</param>
        public async Task<ProductCategoryReadDto?> GetByIdAsync(int id)
        {
            var category = await _context.ProductCategories
                .Include(x => x.CategoryGroup)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null) return null;

            return _mapper.Map<ProductCategoryReadDto>(category);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một danh mục sản phẩm vào hệ thống (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới danh mục.</param>
        /// <returns>ID của danh mục vừa được tạo.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi mã danh mục đã tồn tại hoặc nhóm ngành hàng không hợp lệ.</exception>
        public async Task<int> CreateAsync(ProductCategoryCreateDto dto)
        {
            var upperCode = dto.Code.Trim().ToUpper();

            if (await _context.ProductCategories.AnyAsync(x => x.Code.ToUpper() == upperCode))
                throw new InvalidOperationException($"Mã danh mục '{dto.Code}' đã tồn tại trên hệ thống.");

            if (dto.CategoryGroupId.HasValue)
            {
                var groupExists = await _context.ProductCategoryGroups.AnyAsync(g => g.Id == dto.CategoryGroupId.Value && !g.IsDeleted);
                if (!groupExists)
                    throw new InvalidOperationException("Nhóm ngành hàng được chọn không tồn tại.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<ProductCategory>(dto);
                entity.Code = upperCode;
                entity.Name = dto.Name.Trim();
                entity.Slug = SlugHelper.GenerateSlug(entity.Name);
                entity.Description = dto.Description?.Trim();
                entity.ImagePath = dto.ImagePath?.Trim();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.ProductCategories.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin của một danh mục sản phẩm (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="id">Mã định danh của danh mục cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy danh mục.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi mã danh mục mới bị trùng lặp hoặc nhóm ngành hàng không hợp lệ.</exception>
        public async Task<bool> UpdateAsync(int id, ProductCategoryUpdateDto dto)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục sản phẩm cần cập nhật.");

            var upperCode = dto.Code.Trim().ToUpper();

            if (await _context.ProductCategories.AnyAsync(x => x.Id != id && x.Code.ToUpper() == upperCode))
                throw new InvalidOperationException($"Mã danh mục '{dto.Code}' đã bị trùng lặp với bản ghi khác.");

            if (dto.CategoryGroupId.HasValue)
            {
                var groupExists = await _context.ProductCategoryGroups.AnyAsync(g => g.Id == dto.CategoryGroupId.Value && !g.IsDeleted);
                if (!groupExists)
                    throw new InvalidOperationException("Nhóm ngành hàng được chọn không tồn tại.");
            }

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _mapper.Map(dto, entity);
                entity.Code = upperCode;
                entity.Name = dto.Name.Trim();
                entity.Slug = SlugHelper.GenerateSlug(entity.Name);
                entity.Description = dto.Description?.Trim();
                entity.ImagePath = dto.ImagePath?.Trim();
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa mềm một danh mục sản phẩm (có kiểm tra ràng buộc sản phẩm trực thuộc).
        /// </summary>
        /// <param name="id">Mã định danh của danh mục cần xóa.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy danh mục.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi danh mục đang chứa sản phẩm.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục sản phẩm cần xóa.");

            // Safety Shield: Kiểm tra ràng buộc toàn vẹn dữ liệu
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
            if (hasProducts)
                throw new InvalidOperationException("Không thể xóa danh mục này vì đang có sản phẩm thuộc danh mục.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.ProductCategories.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang hoạt động / Tạm khóa) của danh mục sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của danh mục cần đổi trạng thái.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy danh mục.</exception>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.ProductCategories.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy danh mục sản phẩm.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}
