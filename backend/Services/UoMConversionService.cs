using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Quy tắc quy đổi đơn vị tính (UoM Conversions).
    /// Hỗ trợ thiết lập hệ số quy đổi Tiêu chuẩn (toàn hệ thống) và Đặc thù (theo từng sản phẩm).
    /// </summary>
    public class UoMConversionService : IUoMConversionService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public UoMConversionService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách quy tắc quy đổi kèm thông tin liên kết.
        /// </summary>
        public async Task<IEnumerable<UoMConversionReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.UoMConversions
                .Include(x => x.FromUoM)
                .Include(x => x.ToUoM)
                .Include(x => x.Product)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var conversions = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<UoMConversionReadDto>>(conversions);
        }

        /// <summary>
        /// Tìm kiếm nâng cao và phân trang quy tắc quy đổi.
        /// </summary>
        public async Task<PagedResult<UoMConversionReadDto>> GetPagedAsync(
            string? search,
            int? productId,
            bool? isStandard,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize)
        {
            var query = _context.UoMConversions
                .Include(x => x.FromUoM)
                .Include(x => x.ToUoM)
                .Include(x => x.Product)
                .AsNoTracking()
                .AsQueryable();

            #region Bộ lọc tìm kiếm
            // 1. Filter: Tìm kiếm tổng hợp (Tên đơn vị hoặc thông tin Sản phẩm)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.FromUoM != null && x.FromUoM.Name.ToLower().Contains(lowerSearch)) ||
                    (x.ToUoM != null && x.ToUoM.Name.ToLower().Contains(lowerSearch)) ||
                    (x.Product != null && x.Product.Name.ToLower().Contains(lowerSearch)) ||
                    (x.Product != null && x.Product.Code.ToLower().Contains(lowerSearch))
                );
            }

            // 2. Filter: Lọc theo Sản phẩm hoặc phân loại Tiêu chuẩn/Đặc thù
            if (productId.HasValue)
            {
                query = query.Where(x => x.ProductId == productId.Value);
            }

            if (isStandard.HasValue)
            {
                query = isStandard.Value ? query.Where(x => x.ProductId == null) : query.Where(x => x.ProductId != null);
            }

            // 3. Filter: Trạng thái và Thời gian
            if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

            if (createdAt.HasValue)
            {
                var startDate = createdAt.Value.Date;
                query = query.Where(x => x.CreatedAt >= startDate && x.CreatedAt < startDate.AddDays(1));
            }

            if (updatedAt.HasValue)
            {
                var startDate = updatedAt.Value.Date;
                query = query.Where(x => x.UpdatedAt >= startDate && x.UpdatedAt < startDate.AddDays(1));
            }
            #endregion

            var totalRecords = await query.CountAsync();

            var items = await query
               .OrderByDescending(x => x.CreatedAt)
               .Skip((pageIndex - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync();

            return new PagedResult<UoMConversionReadDto>
            {
                Items = _mapper.Map<IEnumerable<UoMConversionReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<UoMConversionReadDto?> GetByIdAsync(int id)
        {
            var conversion = await _context.UoMConversions
                .Include(x => x.FromUoM)
                .Include(x => x.ToUoM)
                .Include(x => x.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (conversion == null) return null;
            return _mapper.Map<UoMConversionReadDto>(conversion);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo quy tắc quy đổi mới với cơ chế kiểm tra logic chặt chẽ.
        /// </summary>
        public async Task<int> CreateAsync(UoMConversionCreateDto dto)
        {
            // Kiểm tra các ràng buộc toán học và nhóm đơn vị
            await ValidateConversionLogic(dto.FromUoMId, dto.ToUoMId, dto.ProductId, dto.ConversionFactor);

            // Chống trùng lặp quy tắc (Unique constraint tầng ứng dụng)
            var isDuplicate = await _context.UoMConversions
                .AnyAsync(c => c.FromUoMId == dto.FromUoMId &&
                               c.ToUoMId == dto.ToUoMId &&
                               c.ProductId == dto.ProductId &&
                               !c.IsDeleted);

            if (isDuplicate)
                throw new InvalidOperationException("Quy tắc chuyển đổi này đã tồn tại trên hệ thống.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var newConversion = _mapper.Map<UoMConversion>(dto);

                _context.UoMConversions.Add(newConversion);
                await _context.SaveChangesAsync();
                return newConversion.Id;
            });
        }

        /// <summary>
        /// Cập nhật quy tắc quy đổi hiện có.
        /// </summary>
        public async Task<bool> UpdateAsync(int id, UoMConversionUpdateDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var conversion = await _context.UoMConversions.FirstOrDefaultAsync(c => c.Id == id);
                if (conversion == null)
                    throw new KeyNotFoundException($"Không tìm thấy quy tắc quy đổi với ID = {id}.");

                await ValidateConversionLogic(dto.FromUoMId, dto.ToUoMId, dto.ProductId, dto.ConversionFactor);

                // Kiểm tra trùng lặp (loại trừ bản chính nó)
                var isDuplicate = await _context.UoMConversions
                    .AnyAsync(c => c.Id != id &&
                                   c.FromUoMId == dto.FromUoMId &&
                                   c.ToUoMId == dto.ToUoMId &&
                                   c.ProductId == dto.ProductId &&
                                   !c.IsDeleted);

                if (isDuplicate)
                    throw new InvalidOperationException("Cập nhật thất bại: Quy tắc này bị trùng với một thiết lập khác.");

                _mapper.Map(dto, conversion);
                conversion.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var conversion = await _context.UoMConversions.FindAsync(id);
                if (conversion == null)
                    throw new KeyNotFoundException($"Không tìm thấy quy tắc quy đổi với ID = {id}.");

                _context.UoMConversions.Remove(conversion);
                await _context.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var conversion = await _context.UoMConversions.FindAsync(id);
            if (conversion == null)
                throw new KeyNotFoundException($"Không tìm thấy quy tắc quy đổi với ID = {id}.");

            conversion.IsActive = !conversion.IsActive;
            conversion.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return conversion.IsActive;
        }

        #endregion

        #region Private Business Logic Helpers

        /// <summary>
        /// Thẩm định tính hợp lệ về mặt toán học và nghiệp vụ của quy tắc quy đổi.
        /// </summary>
        private async Task ValidateConversionLogic(int fromUoMId, int toUoMId, int? productId, decimal factor)
        {
            // Rule 1: Hệ số phải có ý nghĩa toán học
            if (factor <= 0)
                throw new InvalidOperationException("Hệ số quy đổi phải lớn hơn 0.");

            // Rule 2: Chặn quy đổi vòng lặp (về chính nó)
            if (fromUoMId == toUoMId)
                throw new InvalidOperationException("Lỗi Vòng Lặp: Đơn vị đích không được trùng với đơn vị gốc.");

            // Rule 3: Kiểm tra tính tồn tại của các thực thể liên kết
            if (productId.HasValue && !await _context.Products.AnyAsync(p => p.Id == productId.Value && !p.IsDeleted))
                throw new InvalidOperationException("Sản phẩm được chỉ định không tồn tại hoặc đã bị xóa.");

            var fromUoM = await _context.UoMs.FirstOrDefaultAsync(u => u.Id == fromUoMId && !u.IsDeleted);
            var toUoM = await _context.UoMs.FirstOrDefaultAsync(u => u.Id == toUoMId && !u.IsDeleted);

            if (fromUoM == null || toUoM == null)
                throw new InvalidOperationException("Hệ thống đơn vị tính không hợp lệ hoặc đã bị xóa.");

            // Rule 4: Chặn quy đổi sai hệ quy chiếu (Ví dụ: Không thể đổi Lít sang Mét trừ phi đặc thù sản phẩm)
            // Nếu không có ProductId (Quy đổi chung hệ thống) thì bắt buộc phải cùng CategoryId
            if (!productId.HasValue && fromUoM.CategoryId != toUoM.CategoryId)
                throw new InvalidOperationException("Lỗi Logic: Không thể quy đổi tiêu chuẩn chéo giữa 2 nhóm đơn vị tính khác nhau.");
        }

        #endregion
    }
}