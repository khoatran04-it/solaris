using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.SupplierTypeDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service xử lý logic nghiệp vụ Phân loại Nhà cung cấp (Supplier Type).
    /// </summary>
    public class SupplierTypeService : ISupplierTypeService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public SupplierTypeService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách phân loại nhà cung cấp (không phân trang, phục vụ dropdown)
        /// </summary>
        public async Task<IEnumerable<SupplierTypeReadDto>> GetAllListAsync()
        {
            var types = await _context.SupplierTypes
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SupplierTypeReadDto>>(types);
        }

        /// <summary>
        /// Lấy danh sách phân loại nhà cung cấp có phân trang, tìm kiếm và lọc trạng thái
        /// </summary>
        public async Task<PagedResult<SupplierTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.SupplierTypes.AsQueryable();

            // 1. Filter: Tìm kiếm theo mã hoặc tên
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => 
                    x.Code.ToLower().Contains(lowerSearch) || 
                    x.Name.ToLower().Contains(lowerSearch)
                );
            }

            // 2. Filter: Trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 3. Filter: Theo danh sách tên
            if (!string.IsNullOrWhiteSpace(names))
            {
                var nameList = names.Split(',').Select(n => n.Trim().ToLower()).ToList();
                query = query.Where(x => nameList.Contains(x.Name.ToLower()));
            }

            // 4. Filter: Theo ngày tạo
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

            // 5. Filter: Theo ngày cập nhật
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

            var dtos = _mapper.Map<IEnumerable<SupplierTypeReadDto>>(items);

            return new PagedResult<SupplierTypeReadDto>
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
        public async Task<SupplierTypeReadDto?> GetByIdAsync(int id)
        {
            var type = await _context.SupplierTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (type == null) return null;

            return _mapper.Map<SupplierTypeReadDto>(type);
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Tạo mới phân loại nhà cung cấp
        /// </summary>
        public async Task<int> CreateAsync(SupplierTypeCreateDto dto)
        {
            var trimmedCode = dto.Code.Trim().ToUpper();
            var trimmedName = dto.Name.Trim();

            // Kiểm tra trùng mã code
            if (await _context.SupplierTypes.AnyAsync(x => x.Code.ToUpper() == trimmedCode))
                throw new InvalidOperationException($"Mã phân loại '{dto.Code}' đã tồn tại.");

            if (await _context.SupplierTypes.AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower()))
                throw new InvalidOperationException($"Tên phân loại '{dto.Name}' đã tồn tại.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<SupplierType>(dto);
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                _context.SupplierTypes.Add(entity);
                await _context.SaveChangesAsync();

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin phân loại nhà cung cấp
        /// </summary>
        public async Task<bool> UpdateAsync(int id, SupplierTypeUpdateDto dto)
        {
            var entity = await _context.SupplierTypes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại cần sửa.");

            var trimmedCode = dto.Code.Trim().ToUpper();
            var trimmedName = dto.Name.Trim();

            // Kiểm tra trùng mã code với các bản ghi khác
            if (await _context.SupplierTypes.AnyAsync(x => x.Id != id && x.Code.ToUpper() == trimmedCode))
                throw new InvalidOperationException($"Mã phân loại '{dto.Code}' đã bị trùng lặp với bản ghi khác.");

            if (await _context.SupplierTypes.AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower()))
                throw new InvalidOperationException($"Tên phân loại '{dto.Name}' đã bị trùng lặp với bản ghi khác.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _mapper.Map(dto, entity);
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            });
        }

        /// <summary>
        /// Xóa phân loại nhà cung cấp (chống xóa nếu đang có NCC liên kết)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.SupplierTypes
                .Include(x => x.Suppliers)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại cần xóa.");

            if (entity.Suppliers.Any(s => !s.IsDeleted))
                throw new InvalidOperationException("Không thể xóa phân loại này vì đang có nhà cung cấp trực thuộc.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.SupplierTypes.Remove(entity);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động (Kích hoạt/Tạm khóa) của loại nhà cung cấp.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.SupplierTypes.FindAsync(id);
            if (entity == null) 
                throw new KeyNotFoundException("Không tìm thấy loại nhà cung cấp.");

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