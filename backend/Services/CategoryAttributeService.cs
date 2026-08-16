using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.CategoryAttributeDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class CategoryAttributeService : ICategoryAttributeService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public CategoryAttributeService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryAttributeReadDto>> GetAllListAsync()
        {
            var items = await _context.CategoryAttributes
                .Include(x => x.Category)
                .Include(x => x.AttributeDefinition)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<IEnumerable<CategoryAttributeReadDto>>(items);
        }

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
                var lowerSearch = search.ToLower();
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

        public async Task<int> CreateAsync(CategoryAttributeCreateDto dto)
        {
            // 🔥 Business Rule: Đảm bảo 1 Danh mục không bị gán trùng 1 thuộc tính 2 lần
            var isDuplicate = await _context.CategoryAttributes
                .AnyAsync(x => x.CategoryId == dto.CategoryId && x.AttributeDefinitionId == dto.AttributeDefinitionId);

            if (isDuplicate)
                throw new Exception("Thuộc tính này đã được gán cho danh mục rồi.");

            var entity = _mapper.Map<CategoryAttribute>(dto);

            _context.CategoryAttributes.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, CategoryAttributeUpdateDto dto)
        {
            var entity = await _context.CategoryAttributes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình thuộc tính cần sửa.");

            // 🔥 Bắt lỗi trùng lặp khi Update (ngoại trừ chính nó)
            var isDuplicate = await _context.CategoryAttributes
                .AnyAsync(x => x.Id != id && x.CategoryId == dto.CategoryId && x.AttributeDefinitionId == dto.AttributeDefinitionId);

            if (isDuplicate)
                throw new Exception("Cập nhật thất bại: Thuộc tính này đã tồn tại trong danh mục.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CategoryAttributes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy cấu hình thuộc tính để xóa.");

            // Bảng này không có IsDeleted, nên lệnh Remove này sẽ xóa XÓA CỨNG (Hard Delete) khỏi Database luôn
            _context.CategoryAttributes.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}