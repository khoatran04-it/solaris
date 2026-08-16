using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.ProductDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ProductService : IProductService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public ProductService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProductReadDto>> GetAllListAsync()
        {
            var items = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductReadDto>>(items);
        }

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
                var lowerSearch = search.ToLower();
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

            // 3. Lọc Đa luồng: Đơn vị tính  (BaseToUom)
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
                query = query.Where(x => x.IsActive == isActive.Value);

            // 5. Filter: Ngày tạo (Dùng < endDate để không dính lố giờ)
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
            // Dùng FirstOrDefaultAsync để hỗ trợ .Include()
            var entity = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.BaseUoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            return _mapper.Map<ProductReadDto>(entity);
        }

        public async Task<int> CreateAsync(ProductCreateDto dto)
        {
            // Bắt lỗi trùng mã SKU/Code
            if (await _context.Products.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã sản phẩm này đã tồn tại trong hệ thống.");

            var entity = _mapper.Map<Product>(dto);

            _context.Products.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm cần sửa.");

            // Kiểm tra mã trùng (ngoại trừ chính nó)
            if (await _context.Products.AnyAsync(x => x.Id != id && x.Code == dto.Code))
                throw new Exception("Cập nhật thất bại: Mã sản phẩm này đã bị trùng lặp.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm để xóa.");

            // Xóa mềm
            _context.Products.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

            // Đảo ngược trạng thái
            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<dynamic>> GetDynamicAttributesConfigAsync(int productId)
        {
            // 1. Lấy Sản phẩm để biết nó thuộc Danh mục (Category) nào
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new KeyNotFoundException("Không tìm thấy sản phẩm");

            // 2. Chui vào bảng CategoryAttribute để lấy luật lệ của Danh mục đó
            var attributesConfig = await _context.CategoryAttributes
                .Include(ca => ca.AttributeDefinition)
                // Chỉnh sửa: Bảng CategoryAttribute không có IsActive, 
                // ta sẽ check IsActive của bảng AttributeDefinition (Từ điển)
                .Where(ca => ca.CategoryId == product.CategoryId
                          && ca.AttributeDefinition != null
                          && ca.AttributeDefinition.IsActive)
                .Select(ca => new
                {
                    Id = ca.AttributeDefinitionId,           // ID thuộc tính
                    Name = ca.AttributeDefinition!.Name,     // Tên hiển thị (Size, Quy cách...)
                    IsRequired = ca.IsRequired               // Bắt buộc hay không
                })
                .ToListAsync();

            return attributesConfig;
        }
    }
}