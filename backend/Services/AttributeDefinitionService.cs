using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.AttributeDefinitionDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class AttributeDefinitionService : IAttributeDefinitionService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public AttributeDefinitionService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AttributeDefinitionReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.AttributeDefinitions
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AttributeDefinitionReadDto>>(items);
        }

        public async Task<PagedResult<AttributeDefinitionReadDto>> GetPagedAsync(
            string? search,
            string? dataType,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.AttributeDefinitions.AsQueryable();

            // 1. Filter: Tìm theo tên thuộc tính
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Theo kiểu dữ liệu (Text, Number, Date...)
            if (!string.IsNullOrWhiteSpace(dataType))
            {
                var lowerDataType = dataType.Trim().ToLower();
                query = query.Where(x => x.DataType.ToLower() == lowerDataType);
            }

            // 3. Filter: Trạng thái
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

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

            var dtos = _mapper.Map<IEnumerable<AttributeDefinitionReadDto>>(items);

            return new PagedResult<AttributeDefinitionReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<AttributeDefinitionReadDto> GetByIdAsync(int id)
        {
            var entity = await _context.AttributeDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính.");

            return _mapper.Map<AttributeDefinitionReadDto>(entity);
        }

        public async Task<int> CreateAsync(AttributeDefinitionCreateDto dto)
        {
            var trimmedName = dto.Name.Trim();

            // Bắt lỗi trùng Tên thuộc tính
            var isDuplicate = await _context.AttributeDefinitions
                .AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower());

            if (isDuplicate)
                throw new Exception($"Thuộc tính có tên '{trimmedName}' đã tồn tại trong hệ thống.");

            var entity = _mapper.Map<AttributeDefinition>(dto);
            entity.Name = trimmedName;
            entity.DataType = dto.DataType.Trim().ToUpper();
            entity.IsActive = dto.IsActive;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.AttributeDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, AttributeDefinitionUpdateDto dto)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính cần sửa.");

            var trimmedName = dto.Name.Trim();

            // Bắt lỗi trùng Tên (nhưng bỏ qua chính nó)
            var isDuplicate = await _context.AttributeDefinitions
                .AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower());

            if (isDuplicate)
                throw new Exception($"Cập nhật thất bại: Thuộc tính '{trimmedName}' đã bị trùng với một thuộc tính khác.");

            _mapper.Map(dto, entity);
            entity.Name = trimmedName;
            entity.DataType = dto.DataType.Trim().ToUpper();
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính để xóa.");

            // 1. SAFETY SHIELD: Chặn xóa nếu đang được gắn vào Danh mục
            var isUsedInCategories = await _context.CategoryAttributes
                .AnyAsync(ca => ca.AttributeDefinitionId == id);
            if (isUsedInCategories)
                throw new Exception("Không thể xóa thuộc tính này vì đang được gán vào mẫu danh mục sản phẩm.");

            // 2. SAFETY SHIELD: Chặn xóa nếu đang có Biến thể sản phẩm sử dụng
            var isUsedInVariants = await _context.ProductAttributes
                .AnyAsync(pa => pa.AttributeDefinitionId == id && !pa.IsDeleted);
            if (isUsedInVariants)
                throw new Exception("Không thể xóa thuộc tính này vì đang được sử dụng trong các biến thể sản phẩm.");

            _context.AttributeDefinitions.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}