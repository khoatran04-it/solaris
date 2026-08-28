using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CategoryAttributeDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Cấu hình Thuộc tính cho Danh mục (Category Attribute / EAV Template).
    /// Đảm bảo tính toàn vẹn dữ liệu, kiểm tra tính duy nhất của cấu hình và hỗ trợ giao dịch kiên cường (Execution Strategy).
    /// </summary>
    public class CategoryAttributeService : ICategoryAttributeService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CategoryAttributeService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách cấu hình thuộc tính của các danh mục.
        /// </summary>
        public async Task<IEnumerable<CategoryAttributeReadDto>> GetAllListAsync()
        {
            var items = await _context.CategoryAttributes
                .Include(x => x.Category)
                .Include(x => x.AttributeDefinition)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<CategoryAttributeReadDto>>(items);
        }

        /// <summary>
        /// Lấy danh sách cấu hình thuộc tính có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (theo tên danh mục hoặc tên thuộc tính).</param>
        /// <param name="categoryId">Chuỗi danh sách ID danh mục sản phẩm (phân cách bằng dấu phẩy).</param>
        /// <param name="attributeDefinitionId">Chuỗi danh sách ID định nghĩa thuộc tính (phân cách bằng dấu phẩy).</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        public async Task<PagedResult<CategoryAttributeReadDto>> GetPagedAsync(
            string? search,
            string? categoryId,
            string? attributeDefinitionId,
            int pageIndex,
            int pageSize)
        {
            var query = _context.CategoryAttributes
                .Include(x => x.AttributeDefinition)
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.Category != null && x.Category.Name.ToLower().Contains(lowerSearch)) ||
                    (x.AttributeDefinition != null && x.AttributeDefinition.Name.ToLower().Contains(lowerSearch))
                );
            }

            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                var categoryList = categoryId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (categoryList.Any())
                {
                    query = query.Where(x => categoryList.Contains(x.CategoryId));
                }
            }

            if (!string.IsNullOrWhiteSpace(attributeDefinitionId))
            {
                var attributeList = attributeDefinitionId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (attributeList.Any())
                {
                    query = query.Where(x => x.AttributeDefinitionId.HasValue && attributeList.Contains(x.AttributeDefinitionId.Value));
                }
            }

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<CategoryAttributeReadDto>>(items);

            return new PagedResult<CategoryAttributeReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một bản ghi cấu hình thuộc tính theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cấu hình (CategoryAttribute ID).</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy cấu hình.</exception>
        public async Task<CategoryAttributeReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.CategoryAttributes
                .Include(x => x.Category)
                .Include(x => x.AttributeDefinition)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình thuộc tính danh mục.");

            return _mapper.Map<CategoryAttributeReadDto>(entity);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Thiết lập mới cấu hình: Gán một thuộc tính từ Từ điển hệ thống vào một Danh mục sản phẩm (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="dto">Dữ liệu cấu hình tạo mới.</param>
        /// <returns>ID của bản ghi cấu hình vừa được tạo.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi danh mục, thuộc tính không tồn tại hoặc đã được gán trước đó.</exception>
        public async Task<int> CreateAsync(CategoryAttributeCreateDto dto)
        {
            // 1. Kiểm tra Category tồn tại
            if (!await _context.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted))
                throw new InvalidOperationException("Danh mục sản phẩm được chỉ định không tồn tại.");

            // 2. Kiểm tra Attribute tồn tại
            if (dto.AttributeDefinitionId.HasValue && !await _context.AttributeDefinitions.AnyAsync(a => a.Id == dto.AttributeDefinitionId.Value && !a.IsDeleted))
                throw new InvalidOperationException("Định nghĩa thuộc tính từ điển không tồn tại.");

            // 3. Business Rule: Đảm bảo 1 Danh mục không bị gán trùng 1 thuộc tính 2 lần
            var isDuplicate = await _context.CategoryAttributes
                .AnyAsync(x => x.CategoryId == dto.CategoryId && x.AttributeDefinitionId == dto.AttributeDefinitionId);

            if (isDuplicate)
                throw new InvalidOperationException("Thuộc tính này đã được thiết lập cho danh mục được chọn.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<CategoryAttribute>(dto);

                _context.CategoryAttributes.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật cấu hình thuộc tính của danh mục (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cấu hình cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy cấu hình.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi danh mục, thuộc tính không tồn tại hoặc bị trùng lặp cấu hình.</exception>
        public async Task<bool> UpdateAsync(int id, CategoryAttributeUpdateDto dto)
        {
            var entity = await _context.CategoryAttributes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình thuộc tính cần cập nhật.");

            // 1. Kiểm tra Category tồn tại
            if (!await _context.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted))
                throw new InvalidOperationException("Danh mục sản phẩm được chỉ định không tồn tại.");

            // 2. Kiểm tra Attribute tồn tại
            if (dto.AttributeDefinitionId.HasValue && !await _context.AttributeDefinitions.AnyAsync(a => a.Id == dto.AttributeDefinitionId.Value && !a.IsDeleted))
                throw new InvalidOperationException("Định nghĩa thuộc tính từ điển không tồn tại.");

            // 3. Bắt lỗi trùng lặp khi Update (ngoại trừ chính nó)
            var isDuplicate = await _context.CategoryAttributes
                .AnyAsync(x => x.Id != id && x.CategoryId == dto.CategoryId && x.AttributeDefinitionId == dto.AttributeDefinitionId);

            if (isDuplicate)
                throw new InvalidOperationException("Cập nhật thất bại: Thuộc tính này đã tồn tại trong danh mục.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _mapper.Map(dto, entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa bỏ cấu hình: Hủy gán thuộc tính khỏi danh mục sản phẩm (sử dụng Transaction Resilience).
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cấu hình cần xóa.</param>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy cấu hình.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CategoryAttributes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình thuộc tính để xóa.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.CategoryAttributes.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion
    }
}