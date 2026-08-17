using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Sản Phẩm Gốc (Product Master).
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách sản phẩm không phân trang.
        /// </summary>
        public async Task<IEnumerable<ProductReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.Products
                .Include(x => x.Category)
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductReadDto>>(items);
        }

        /// <summary>
        /// Lấy danh sách sản phẩm có phân trang, tìm kiếm và bộ lọc đa luồng.
        /// </summary>
        public async Task<PagedResult<ProductReadDto>> GetPagedAsync(
            string? search,
            string? categoryId,
            string? baseUoMId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Products
                .Include(x => x.Category)
                .Include(x => x.BaseUoM)
                .AsQueryable();

            // 1. Filter: Tìm kiếm mã hoặc tên
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch));
            }

            // 2. Lọc Đa luồng: Danh Mục Sản Phẩm (Category)
            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                var categoryIdList = categoryId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (categoryIdList.Any())
                {
                    query = query.Where(x => x.CategoryId.HasValue && categoryIdList.Contains(x.CategoryId.Value));
                }
            }

            // 3. Lọc Đa luồng: Đơn vị tính cơ sở (BaseUoM)
            if (!string.IsNullOrWhiteSpace(baseUoMId))
            {
                var uomList = baseUoMId.Split(',')
                    .Where(idStr => int.TryParse(idStr.Trim(), out _))
                    .Select(idStr => int.Parse(idStr.Trim()))
                    .ToList();

                if (uomList.Any())
                {
                    query = query.Where(x => uomList.Contains(x.BaseUoMId));
                }
            }

            // 4. Filter: Theo trạng thái Kích hoạt
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 5. Filter: Ngày tạo (So sánh 24h trọn vẹn)
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 6. Filter: Ngày cập nhật
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

            var dtos = _mapper.Map<IEnumerable<ProductReadDto>>(items);

            return new PagedResult<ProductReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<ProductReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            return _mapper.Map<ProductReadDto>(entity);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        public async Task<int> CreateAsync(ProductCreateDto dto)
        {
            var trimmedCode = dto.Code.Trim();

            // 1. Kiểm tra trùng mã Code
            if (await _context.Products.AnyAsync(x => x.Code == trimmedCode))
                throw new Exception($"Mã sản phẩm '{trimmedCode}' đã tồn tại trong hệ thống.");

            // 2. Kiểm tra tính hợp lệ của BaseUoMId
            if (!await _context.UoMs.AnyAsync(u => u.Id == dto.BaseUoMId && !u.IsDeleted))
                throw new Exception("Đơn vị tính cơ sở không tồn tại.");

            // 3. Kiểm tra CategoryId nếu có
            if (dto.CategoryId.HasValue && !await _context.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId.Value && !c.IsDeleted))
                throw new Exception("Danh mục sản phẩm không tồn tại.");

            var entity = _mapper.Map<Product>(dto);
            entity.Code = trimmedCode;
            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description?.Trim();
            entity.ImagePath = dto.ImagePath?.Trim();
            entity.IsActive = dto.IsActive;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.Products.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm cần sửa.");

            var trimmedCode = dto.Code.Trim();

            // 1. Kiểm tra mã trùng (ngoại trừ chính nó)
            if (await _context.Products.AnyAsync(x => x.Id != id && x.Code == trimmedCode))
                throw new Exception($"Cập nhật thất bại: Mã sản phẩm '{trimmedCode}' đã bị trùng lặp.");

            // 2. Kiểm tra tính hợp lệ của BaseUoMId
            if (!await _context.UoMs.AnyAsync(u => u.Id == dto.BaseUoMId && !u.IsDeleted))
                throw new Exception("Đơn vị tính cơ sở không tồn tại.");

            // 3. Kiểm tra CategoryId nếu có
            if (dto.CategoryId.HasValue && !await _context.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId.Value && !c.IsDeleted))
                throw new Exception("Danh mục sản phẩm không tồn tại.");

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

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm để xóa.");

            // 1. SAFETY SHIELD: Chặn xóa nếu đang có biến thể SKU trực thuộc
            var hasVariants = await _context.ProductVariants.AnyAsync(v => v.ProductId == id && !v.IsDeleted);
            if (hasVariants)
                throw new Exception("Không thể xóa sản phẩm này vì đang có các biến thể (SKU) trực thuộc.");

            // 2. SAFETY SHIELD: Chặn xóa nếu đang có quy tắc quy đổi đặc thù
            var hasConversions = await _context.UoMConversions.AnyAsync(c => c.ProductId == id && !c.IsDeleted);
            if (hasConversions)
                throw new Exception("Không thể xóa sản phẩm này vì đang có quy tắc quy đổi đơn vị liên kết.");

            _context.Products.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<dynamic>> GetDynamicAttributesConfigAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            if (!product.CategoryId.HasValue)
                return Enumerable.Empty<dynamic>();

            var attributesConfig = await _context.CategoryAttributes
                .Include(ca => ca.AttributeDefinition)
                .Where(ca => ca.CategoryId == product.CategoryId.Value
                          && ca.AttributeDefinition != null
                          && ca.AttributeDefinition.IsActive)
                .Select(ca => new
                {
                    Id = ca.AttributeDefinitionId,
                    Name = ca.AttributeDefinition!.Name,
                    IsRequired = ca.IsRequired
                })
                .ToListAsync();

            return attributesConfig;
        }

        #endregion
    }
}