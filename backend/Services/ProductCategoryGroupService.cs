using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductCategoryGroupDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Nhóm Ngành Hàng (Product Category Groups).
    /// Đảm bảo tính toàn vẹn dữ liệu, kiểm tra tính duy nhất (case-insensitive) và hỗ trợ giao dịch kiên cường (Execution Strategy).
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

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm ngành hàng (không phân trang, thường dùng cho giao diện Dropdown/Select).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các nhóm ngành hàng đang ở trạng thái hoạt động.</param>
        public async Task<IEnumerable<ProductCategoryGroupReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.ProductCategoryGroups.AsNoTracking().AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var groups = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductCategoryGroupReadDto>>(groups);
        }

        /// <summary>
        /// Lấy danh sách nhóm ngành hàng có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã hoặc tên nhóm ngành hàng.</param>
        /// <param name="names">Lọc theo một danh sách tên cụ thể (phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
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
                var nameList = names.Split(',')
                    .Select(n => n.Trim().ToLower())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (nameList.Any())
                {
                    query = query.Where(x => nameList.Contains(x.Name.ToLower()));
                }
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
        /// Lấy thông tin chi tiết của một nhóm ngành hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng.</param>
        public async Task<ProductCategoryGroupReadDto?> GetByIdAsync(int id)
        {
            var group = await _context.ProductCategoryGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (group == null) return null;

            return _mapper.Map<ProductCategoryGroupReadDto>(group);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một nhóm ngành hàng vào hệ thống (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới nhóm ngành hàng.</param>
        /// <returns>ID của nhóm ngành hàng vừa được tạo.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi mã nhóm ngành hàng đã tồn tại.</exception>
        public async Task<int> CreateAsync(ProductCategoryGroupCreateDto dto)
        {
            var upperCode = dto.Code.Trim().ToUpper();

            if (await _context.ProductCategoryGroups.AnyAsync(x => x.Code.ToUpper() == upperCode))
                throw new InvalidOperationException($"Mã nhóm ngành hàng '{dto.Code}' đã tồn tại trên hệ thống.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<ProductCategoryGroup>(dto);
                entity.Code = upperCode;
                entity.Name = dto.Name.Trim();
                entity.Description = dto.Description?.Trim();
                entity.ImagePath = dto.ImagePath?.Trim();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.ProductCategoryGroups.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin của một nhóm ngành hàng (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy nhóm ngành hàng.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi mã nhóm ngành hàng mới bị trùng với bản ghi khác.</exception>
        public async Task<bool> UpdateAsync(int id, ProductCategoryGroupUpdateDto dto)
        {
            var entity = await _context.ProductCategoryGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm ngành hàng cần cập nhật.");

            var upperCode = dto.Code.Trim().ToUpper();

            if (await _context.ProductCategoryGroups.AnyAsync(x => x.Id != id && x.Code.ToUpper() == upperCode))
                throw new InvalidOperationException($"Mã nhóm ngành hàng '{dto.Code}' đã bị trùng lặp với bản ghi khác.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _mapper.Map(dto, entity);
                entity.Code = upperCode;
                entity.Name = dto.Name.Trim();
                entity.Description = dto.Description?.Trim();
                entity.ImagePath = dto.ImagePath?.Trim();
                entity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa mềm một nhóm ngành hàng (có kiểm tra ràng buộc danh mục con trực thuộc).
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng cần xóa.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy nhóm ngành hàng.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi nhóm ngành hàng đang chứa danh mục con.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ProductCategoryGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm ngành hàng cần xóa.");

            // Safety Shield: Kiểm tra ràng buộc toàn vẹn dữ liệu
            bool hasCategories = await _context.ProductCategories.AnyAsync(c => c.CategoryGroupId == id && !c.IsDeleted);
            if (hasCategories)
                throw new InvalidOperationException("Không thể xóa nhóm ngành hàng này vì đang chứa các danh mục hàng hóa trực thuộc.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.ProductCategoryGroups.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang hoạt động / Tạm khóa) của nhóm ngành hàng.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng cần đổi trạng thái.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy nhóm ngành hàng.</exception>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.ProductCategoryGroups.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy nhóm ngành hàng.");

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