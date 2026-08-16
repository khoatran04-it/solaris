using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.UoMDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Đơn vị tính (Unit of Measure - UoM).
    /// Xử lý các quy tắc chuyển đổi, ràng buộc giữa Đơn vị tính và Nhóm đơn vị (UoM Category).
    /// </summary>
    public class UoMService : IUoMService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public UoMService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách toàn bộ đơn vị tính kèm theo thông tin nhóm chủ quản.
        /// </summary>
        public async Task<IEnumerable<UoMReadDto>> GetAllListAsync()
        {
            var uoms = await _context.UoMs
                .Include(x => x.Category) // Nạp thông tin Nhóm để hiển thị CategoryName
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UoMReadDto>>(uoms);
        }

        /// <summary>
        /// Tìm kiếm và phân trang đơn vị tính với các bộ lọc linh hoạt.
        /// </summary>
        /// <remarks>
        /// Tối ưu SQL: Đã loại bỏ AsSplitQuery() vì chỉ Include quan hệ 1-1, 
        /// giúp thực hiện JOIN tối ưu trên một câu lệnh duy nhất.
        /// </remarks>
        public async Task<PagedResult<UoMReadDto>> GetPagedAsync(
            string? search,
            int? categoryId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.UoMs
                .Include(x => x.Category)
                .AsQueryable();

            // 1. Filter: Tìm kiếm theo Mã, Tên hoặc Từ đồng nghĩa (Hỗ trợ AI Search/Keywords)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(x =>
                    x.Code.ToLower().Contains(lowerSearch) ||
                    x.Name.ToLower().Contains(lowerSearch) ||
                    (x.Synonyms != null && x.Synonyms.ToLower().Contains(lowerSearch)));
            }

            // 2. Filter: Lọc theo Nhóm chủ quản
            if (categoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == categoryId.Value);
            }

            // 3. Filter: Trạng thái hoạt động
            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // 4. Filter: Theo thời gian (So sánh trọn ngày 24h)
            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                var endDate = startDate.AddDays(1);
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < endDate);
            }

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

            var dtos = _mapper.Map<IEnumerable<UoMReadDto>>(items);

            return new PagedResult<UoMReadDto>
            {
                Items = dtos,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy chi tiết đơn vị tính theo ID định danh.
        /// </summary>
        public async Task<UoMReadDto?> GetByIdAsync(int id)
        {
            var uom = await _context.UoMs
                .Include(x => x.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (uom == null) return null;

            return _mapper.Map<UoMReadDto>(uom);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một đơn vị tính. Đảm bảo mã Code và Nhóm tồn tại hợp lệ.
        /// </summary>
        public async Task<int> CreateAsync(UoMCreateDto dto)
        {
            // Kiểm tra trùng Mã đơn vị (Unique Code)
            if (await _context.UoMs.AnyAsync(c => c.Code == dto.Code))
                throw new Exception("Mã đơn vị tính đã tồn tại trên hệ thống.");

            // Kiểm tra tính hợp lệ của Nhóm chủ quản
            if (!await _context.UoMCategories.AnyAsync(c => c.Id == dto.CategoryId))
                throw new Exception("Nhóm đơn vị tính không tồn tại.");

            var newUoM = _mapper.Map<UoM>(dto);
            _context.UoMs.Add(newUoM);

            await _context.SaveChangesAsync();
            return newUoM.Id;
        }

        /// <summary>
        /// Cập nhật thông tin đơn vị tính và kiểm tra các ràng buộc liên kết.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, UoMUpdateDto dto)
        {
            var uom = await _context.UoMs.FirstOrDefaultAsync(c => c.Id == id);
            if (uom == null)
                throw new KeyNotFoundException("Không tìm thấy dữ liệu yêu cầu.");

            // Kiểm tra trùng mã Code (loại trừ bản ghi hiện tại)
            if (await _context.UoMs.AnyAsync(c => c.Id != id && c.Code == dto.Code))
                throw new Exception("Cập nhật thất bại: Mã đơn vị tính này đã bị trùng lặp.");

            // Đảm bảo Nhóm mới cập nhật là hợp lệ
            if (!await _context.UoMCategories.AnyAsync(c => c.Id == dto.CategoryId))
                throw new Exception("Nhóm đơn vị tính mới không tồn tại.");

            _mapper.Map(dto, uom);
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Xóa đơn vị tính khỏi hệ thống.
        /// </summary>
        /// <exception cref="Exception">Ném ra khi đơn vị này đang là Đơn vị cơ sở của một Nhóm.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            var uom = await _context.UoMs.FindAsync(id);
            if (uom == null) throw new KeyNotFoundException("Không tìm thấy dữ liệu để xóa.");

            // SAFETY SHIELD: Ngăn chặn xóa đơn vị đang đóng vai trò là "Gốc/Cơ sở" (Base UoM) của một Category
            var isUsedAsBase = await _context.UoMCategories.AnyAsync(c => c.BaseUoMId == id);
            if (isUsedAsBase)
                throw new Exception("Không thể xóa: Đơn vị này đang được thiết lập làm Đơn vị gốc cho một nhóm đơn vị.");

            _context.UoMs.Remove(uom);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động (Kích hoạt/Khóa) của đơn vị tính.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var uom = await _context.UoMs.FindAsync(id);
            if (uom == null) throw new KeyNotFoundException("Không tìm thấy đơn vị tính.");

            uom.IsActive = !uom.IsActive;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}