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

        public async Task<IEnumerable<AttributeDefinitionReadDto>> GetAllListAsync()
        {
            var items = await _context.AttributeDefinitions
                .AsNoTracking()
                .OrderBy(x => x.Name) // Sắp xếp theo vần A-Z cho dễ tìm
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
                var lowerSearch = search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerSearch));
            }

            // 2. Filter: Theo kiểu dữ liệu (Text, Number, Date...)
            if (!string.IsNullOrWhiteSpace(dataType))
            {
                var lowerDataType = dataType.ToLower();
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
            // 🔥 Bắt lỗi trùng Tên thuộc tính
            var isDuplicate = await _context.AttributeDefinitions
                .AnyAsync(x => x.Name.ToLower() == dto.Name.ToLower());

            if (isDuplicate)
                throw new Exception($"Thuộc tính có tên '{dto.Name}' đã tồn tại trong hệ thống.");

            var entity = _mapper.Map<AttributeDefinition>(dto);

            _context.AttributeDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, AttributeDefinitionUpdateDto dto)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính cần sửa.");

            // 🔥 Bắt lỗi trùng Tên (nhưng bỏ qua chính nó)
            var isDuplicate = await _context.AttributeDefinitions
                .AnyAsync(x => x.Id != id && x.Name.ToLower() == dto.Name.ToLower());

            if (isDuplicate)
                throw new Exception($"Cập nhật thất bại: Thuộc tính '{dto.Name}' đã bị trùng với một thuộc tính khác.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.AttributeDefinitions.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy từ điển thuộc tính để xóa.");

            // Ở DB context sếp đã gài Restrict với CategoryAttribute
            // Nên nếu thuộc tính này đang được dùng ở Khung danh mục, lúc SaveChanges sẽ tự văng lỗi
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
            await _context.SaveChangesAsync();

            return true;
        }
    }
}