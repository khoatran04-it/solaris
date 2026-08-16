using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.SupplierTypeDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class SupplierTypeService : ISupplierTypeService
    {
        // ==========================================
        // SECTION: FIELDS & CONSTRUCTOR
        // ==========================================
        #region Fields & Constructor

        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public SupplierTypeService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #endregion


        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách phân loại nhà cung cấp (không phân trang)
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
        /// Lấy danh sách phân loại nhà cung cấp có phân trang và tìm kiếm
        /// </summary>
        public async Task<PagedResult<SupplierTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.SupplierTypes.AsQueryable();

            // 1. Filter: Tìm kiếm theo mã hoặc tên
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
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

            // Thực hiện phân trang và tối ưu hóa truy vấn
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
                .FindAsync(id);

            if (type == null) return null;

            return _mapper.Map<SupplierTypeReadDto>(type);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới phân loại nhà cung cấp
        /// </summary>
        public async Task<int> CreateAsync(SupplierTypeCreateDto dto)
        {
            // Kiểm tra trùng mã code
            if (await _context.SupplierTypes.AnyAsync(x => x.Code == dto.Code))
                throw new Exception("Mã phân loại đã tồn tại.");

            var entity = _mapper.Map<SupplierType>(dto);

            _context.SupplierTypes.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        /// <summary>
        /// Cập nhật thông tin phân loại nhà cung cấp
        /// </summary>
        public async Task<bool> UpdateAsync(int id, SupplierTypeUpdateDto dto)
        {
            var entity = await _context.SupplierTypes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại cần sửa.");

            // Kiểm tra trùng mã code với các bản ghi khác
            if (await _context.SupplierTypes.AnyAsync(x => x.Id != id && x.Code == dto.Code))
                throw new Exception("Cập nhật thất bại: Mã phân loại này đã bị trùng lặp với một dòng dữ liệu khác.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa phân loại nhà cung cấp
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.SupplierTypes.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy phân loại cần xóa.");

            // Thao tác Remove sẽ được Global Interceptor chuyển thành Update DeletedAt nếu dùng Soft Delete
            _context.SupplierTypes.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}