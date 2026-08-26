using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.UoMDTOs;
using backend.Helpers;
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

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy danh sách toàn bộ đơn vị tính kèm theo thông tin nhóm chủ quản.
        /// </summary>
        public async Task<IEnumerable<UoMReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.UoMs
                .Include(x => x.Category)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var uoms = await query
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UoMReadDto>>(uoms);
        }

        /// <summary>
        /// Tìm kiếm và phân trang đơn vị tính với các bộ lọc linh hoạt.
        /// </summary>
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
                .AsNoTracking()
                .AsQueryable();

            #region Bộ lọc tìm kiếm
            // 1. Filter: Tìm kiếm theo Mã, Tên hoặc Từ đồng nghĩa (Hỗ trợ AI Search/Keywords)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
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
            #endregion

            var totalRecords = await query.CountAsync();

            var items = await query
               .OrderByDescending(x => x.CreatedAt)
               .Skip((pageIndex - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync();

            return new PagedResult<UoMReadDto>
            {
                Items = _mapper.Map<IEnumerable<UoMReadDto>>(items),
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

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một đơn vị tính. Đảm bảo mã Code và Nhóm tồn tại hợp lệ.
        /// </summary>
        public async Task<int> CreateAsync(UoMCreateDto dto)
        {
            var normalizedCode = dto.Code.Trim().ToUpper();

            // Kiểm tra trùng Mã đơn vị (Unique Code không phân biệt hoa thường)
            if (await _context.UoMs.AnyAsync(c => c.Code.ToUpper() == normalizedCode))
                throw new InvalidOperationException($"Mã đơn vị tính '{dto.Code.Trim()}' đã tồn tại trong hệ thống.");

            // Kiểm tra tính hợp lệ của Nhóm chủ quản
            if (!await _context.UoMCategories.AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted))
                throw new InvalidOperationException("Nhóm đơn vị tính không tồn tại hoặc đã bị xóa.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var newUoM = _mapper.Map<UoM>(dto);

                _context.UoMs.Add(newUoM);
                await _context.SaveChangesAsync();
                return newUoM.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin đơn vị tính và kiểm tra các ràng buộc liên kết.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, UoMUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var uom = await _context.UoMs.FirstOrDefaultAsync(c => c.Id == id);
                if (uom == null)
                    throw new KeyNotFoundException($"Không tìm thấy đơn vị tính với ID = {id}.");

                var normalizedCode = dto.Code.Trim().ToUpper();

                // Kiểm tra trùng mã Code (loại trừ bản ghi hiện tại)
                if (await _context.UoMs.AnyAsync(c => c.Id != id && c.Code.ToUpper() == normalizedCode))
                    throw new InvalidOperationException($"Cập nhật thất bại: Mã đơn vị tính '{dto.Code.Trim()}' đã bị trùng lặp.");

                // Đảm bảo Nhóm mới cập nhật là hợp lệ
                if (!await _context.UoMCategories.AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted))
                    throw new InvalidOperationException("Nhóm đơn vị tính mới không tồn tại hoặc đã bị xóa.");

                _mapper.Map(dto, uom);
                uom.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa đơn vị tính khỏi hệ thống (với các lớp bảo vệ toàn vẹn dữ liệu).
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var uom = await _context.UoMs.FindAsync(id);
                if (uom == null)
                    throw new KeyNotFoundException($"Không tìm thấy đơn vị tính với ID = {id}.");

                // 1. SAFETY SHIELD: Ngăn chặn xóa đơn vị đang đóng vai trò là "Gốc/Cơ sở" (Base UoM) của một Category
                var isUsedAsBase = await _context.UoMCategories.AnyAsync(c => c.BaseUoMId == id && !c.IsDeleted);
                if (isUsedAsBase)
                    throw new InvalidOperationException("Không thể xóa: Đơn vị này đang được thiết lập làm Đơn vị gốc cho một nhóm đơn vị.");

                // 2. SAFETY SHIELD: Ngăn chặn xóa nếu Sản phẩm đang dùng đơn vị này làm BaseUoM
                var isUsedInProduct = await _context.Products.AnyAsync(p => p.BaseUoMId == id && !p.IsDeleted);
                if (isUsedInProduct)
                    throw new InvalidOperationException("Không thể xóa: Đơn vị này đang được dùng làm đơn vị cơ sở cho sản phẩm.");

                // 3. SAFETY SHIELD: Ngăn chặn xóa nếu có quy tắc quy đổi nào liên quan
                var isUsedInConversion = await _context.UoMConversions.AnyAsync(c => (c.FromUoMId == id || c.ToUoMId == id) && !c.IsDeleted);
                if (isUsedInConversion)
                    throw new InvalidOperationException("Không thể xóa: Đang có quy tắc quy đổi liên kết với đơn vị tính này.");

                // 4. SAFETY SHIELD: Ngăn chặn xóa nếu Nhà cung cấp đang dùng đơn vị này
                var isUsedInSupplierProduct = await _context.SupplierProducts.AnyAsync(sp => sp.PurchaseUoMId == id && !sp.IsDeleted);
                if (isUsedInSupplierProduct)
                    throw new InvalidOperationException("Không thể xóa: Đang có nhà cung cấp sử dụng đơn vị tính mua hàng này.");

                _context.UoMs.Remove(uom);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động (Kích hoạt/Khóa) của đơn vị tính.
        /// </summary>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            var uom = await _context.UoMs.FindAsync(id);
            if (uom == null)
                throw new KeyNotFoundException($"Không tìm thấy đơn vị tính với ID = {id}.");

            uom.IsActive = !uom.IsActive;
            uom.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return uom.IsActive;
        }

        #endregion
    }
}