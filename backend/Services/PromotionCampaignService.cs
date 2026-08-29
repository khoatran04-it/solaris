using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.PromotionCampaignDTOs;
using backend.Helpers;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    /// <summary>
    /// Service quản lý Chiến Dịch Khuyến Mãi (Promotion Campaign) và liên kết Biến thể sản phẩm (SKU).
    /// Đảm bảo tính toán toàn vẹn các chương trình khuyến mãi, bọc toàn bộ mutations trong Transaction,
    /// tự động sinh SEO slug và đồng bộ danh sách sản phẩm tham gia sale.
    /// </summary>
    public class PromotionCampaignService : IPromotionCampaignService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        /// <summary>
        /// Khởi tạo Service với DbContext và AutoMapper.
        /// </summary>
        /// <param name="context">Database context của hệ thống Solaris.</param>
        /// <param name="mapper">Bộ ánh xạ AutoMapper.</param>
        public PromotionCampaignService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách Chiến dịch (không phân trang).
        /// Thường dùng cho các bộ lọc hoặc Dropdown list trên giao diện Quản trị.
        /// </summary>
        /// <param name="isActiveOnly">Nếu true, chỉ lấy các chiến dịch đang Kích hoạt. Nếu false, lấy tất cả.</param>
        /// <returns>Danh sách các chiến dịch khuyến mãi.</returns>
        public async Task<IEnumerable<PromotionCampaignReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.PromotionCampaigns
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Product)
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Prices)
                            .ThenInclude(p => p.UoM)
                .AsNoTracking()
                .AsQueryable();

            if (isActiveOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PromotionCampaignReadDto>>(items);
        }

        /// <summary>
        /// Lấy danh sách Chiến dịch có hỗ trợ phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (theo tên chiến dịch).</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang hoạt động, false: Tạm dừng).</param>
        /// <param name="startDate">Lọc các chiến dịch bắt đầu từ khoảng thời gian này.</param>
        /// <param name="endDate">Lọc các chiến dịch kết thúc trong khoảng thời gian này.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        /// <returns>Kết quả phân trang chứa danh sách chiến dịch và metadata.</returns>
        public async Task<PagedResult<PromotionCampaignReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize)
        {
            var query = _context.PromotionCampaigns
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Product)
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Prices)
                            .ThenInclude(p => p.UoM)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerSearch));
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            // Lọc chiến dịch theo khoảng thời gian bắt đầu / kết thúc
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(x => x.StartDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                query = query.Where(x => x.EndDate < end);
            }

            var totalRecords = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedResult<PromotionCampaignReadDto>
            {
                Items = _mapper.Map<IEnumerable<PromotionCampaignReadDto>>(items),
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một Chiến dịch theo ID kèm danh sách sản phẩm tham gia.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch.</param>
        /// <returns>DTO chi tiết chiến dịch hoặc null nếu không tìm thấy.</returns>
        public async Task<PromotionCampaignReadDto?> GetByIdAsync(int id)
        {
            var entity = await _context.PromotionCampaigns
                // Nhánh 1: Kéo Variant -> Product gốc
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Product)
                // Nhánh 2: Kéo Variant -> Prices -> UoM
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Prices)
                            .ThenInclude(p => p.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return null;

            return _mapper.Map<PromotionCampaignReadDto>(entity);
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một vỏ Chiến dịch khuyến mãi và gắn danh sách biến thể khởi tạo (nếu có).
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới chiến dịch.</param>
        /// <returns>ID của chiến dịch vừa được tạo thành công.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi dữ liệu đầu vào vi phạm quy tắc nghiệp vụ hoặc trùng tên chiến dịch.</exception>
        public async Task<int> CreateAsync(PromotionCampaignCreateDto dto)
        {
            var trimmedName = dto.Name?.Trim() ?? string.Empty;

            // 1. Validation Logic
            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new InvalidOperationException("Tên chiến dịch khuyến mãi không được để trống.");

            if (dto.StartDate >= dto.EndDate)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc chiến dịch.");

            if (dto.IsPercentage && (dto.DiscountValue <= 0 || dto.DiscountValue > 100))
                throw new InvalidOperationException("Mức giảm theo phần trăm phải nằm trong khoảng từ 0.01% đến 100%.");

            if (!dto.IsPercentage && dto.DiscountValue <= 0)
                throw new InvalidOperationException("Mức giảm tiền mặt phải lớn hơn 0.");

            var isDuplicate = await _context.PromotionCampaigns
                .AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower());
            if (isDuplicate)
                throw new InvalidOperationException($"Chiến dịch khuyến mãi '{trimmedName}' đã tồn tại trong hệ thống.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = _mapper.Map<PromotionCampaign>(dto);
                entity.Name = trimmedName;
                entity.Slug = SlugHelper.GenerateSlug(trimmedName);
                entity.Description = dto.Description?.Trim();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsActive = dto.IsActive;

                _context.PromotionCampaigns.Add(entity);
                await _context.SaveChangesAsync();

                // Nếu có truyền danh sách biến thể ngay lúc tạo
                if (dto.VariantIds != null && dto.VariantIds.Any())
                {
                    var validVariantIds = await _context.ProductVariants
                        .Where(v => dto.VariantIds.Contains(v.Id) && !v.IsDeleted)
                        .Select(v => v.Id)
                        .ToListAsync();

                    var links = validVariantIds.Distinct().Select(vId => new PromotionVariant
                    {
                        PromotionCampaignId = entity.Id,
                        VariantId = vId,
                        CreatedAt = DateTime.UtcNow
                    });
                    _context.PromotionVariants.AddRange(links);
                    await _context.SaveChangesAsync();
                }

                return entity.Id;
            });
        }

        /// <summary>
        /// Cập nhật thông tin cấu hình vỏ Chiến dịch khuyến mãi.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy chiến dịch theo ID.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi dữ liệu không hợp lệ hoặc trùng tên với chiến dịch khác.</exception>
        public async Task<bool> UpdateAsync(int id, PromotionCampaignUpdateDto dto)
        {
            var trimmedName = dto.Name?.Trim() ?? string.Empty;

            // 1. Validation Logic
            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new InvalidOperationException("Tên chiến dịch khuyến mãi không được để trống.");

            if (dto.StartDate >= dto.EndDate)
                throw new InvalidOperationException("Ngày bắt đầu phải trước ngày kết thúc chiến dịch.");

            if (dto.IsPercentage && (dto.DiscountValue <= 0 || dto.DiscountValue > 100))
                throw new InvalidOperationException("Mức giảm theo phần trăm phải nằm trong khoảng từ 0.01% đến 100%.");

            if (!dto.IsPercentage && dto.DiscountValue <= 0)
                throw new InvalidOperationException("Mức giảm tiền mặt phải lớn hơn 0.");

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.PromotionCampaigns.FindAsync(id);
                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi cần sửa.");

                var isDuplicate = await _context.PromotionCampaigns
                    .AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower());
                if (isDuplicate)
                    throw new InvalidOperationException($"Cập nhật thất bại: Tên chiến dịch '{trimmedName}' đã bị trùng.");

                _mapper.Map(dto, entity);
                entity.Name = trimmedName;
                entity.Slug = SlugHelper.GenerateSlug(trimmedName);
                entity.Description = dto.Description?.Trim();
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsActive = dto.IsActive;

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Xóa (mềm) một Chiến dịch khuyến mãi và dọn dẹp liên kết sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch cần xóa.</param>
        /// <returns>True nếu xóa thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy chiến dịch theo ID.</exception>
        public async Task<bool> DeleteAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.PromotionCampaigns
                    .Include(x => x.PromotionVariants)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy chiến dịch để xóa.");

                // Soft delete chiến dịch
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                // Dọn dẹp liên kết PromotionVariants khi chiến dịch bị xóa
                if (entity.PromotionVariants.Any())
                {
                    _context.PromotionVariants.RemoveRange(entity.PromotionVariants);
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Chuyển đổi trạng thái kích hoạt (Hoạt động / Tạm dừng) của Chiến dịch khuyến mãi.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy chiến dịch theo ID.</exception>
        public async Task<bool> ToggleActiveAsync(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var entity = await _context.PromotionCampaigns.FindAsync(id);
                if (entity == null)
                    throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi.");

                entity.IsActive = !entity.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            });
        }

        #endregion

        // ==========================================
        // SECTION: VARIANT MAPPING OPERATIONS
        // ==========================================
        #region Variant Mapping Operations

        /// <summary>
        /// Bổ sung danh sách các biến thể sản phẩm vào chiến dịch khuyến mãi.
        /// </summary>
        /// <param name="campaignId">Mã định danh của chiến dịch.</param>
        /// <param name="dto">Danh sách ID các biến thể (SKU) cần thêm vào.</param>
        /// <returns>True nếu quá trình thêm mới thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy chiến dịch theo ID.</exception>
        public async Task<bool> AddVariantsToCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var campaignExists = await _context.PromotionCampaigns.AnyAsync(x => x.Id == campaignId);
                if (!campaignExists)
                    throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi.");

                var targetVariantIds = dto.VariantIds ?? new List<int>();

                // Lấy danh sách các sản phẩm đang được áp dụng KM hiện tại trong DB
                var currentLinks = await _context.PromotionVariants
                    .Where(x => x.PromotionCampaignId == campaignId)
                    .ToListAsync();

                var currentVariantIds = currentLinks.Select(x => x.VariantId).ToList();

                // 1. Tìm những SP bị người dùng "Bỏ tích"
                var toRemove = currentLinks.Where(x => !targetVariantIds.Contains(x.VariantId)).ToList();

                // 2. Tìm những SP được "Tích mới" (đảm bảo tồn tại và chưa bị xóa)
                var newVariantIds = targetVariantIds.Where(vId => !currentVariantIds.Contains(vId)).Distinct().ToList();

                var validVariantIds = await _context.ProductVariants
                    .Where(v => newVariantIds.Contains(v.Id) && !v.IsDeleted)
                    .Select(v => v.Id)
                    .ToListAsync();

                var toAdd = validVariantIds.Select(vId => new PromotionVariant
                {
                    PromotionCampaignId = campaignId,
                    VariantId = vId,
                    CreatedAt = DateTime.UtcNow
                });

                // 3. Thực thi Xóa và Thêm
                if (toRemove.Any()) _context.PromotionVariants.RemoveRange(toRemove);
                if (toAdd.Any()) _context.PromotionVariants.AddRange(toAdd);

                await _context.SaveChangesAsync();
                return true;
            });
        }

        /// <summary>
        /// Gỡ bỏ danh sách các biến thể sản phẩm khỏi chiến dịch khuyến mãi.
        /// </summary>
        /// <param name="campaignId">Mã định danh của chiến dịch.</param>
        /// <param name="dto">Danh sách ID các biến thể (SKU) cần gỡ bỏ.</param>
        /// <returns>True nếu quá trình gỡ bỏ thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy chiến dịch theo ID.</exception>
        public async Task<bool> RemoveVariantsFromCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var campaignExists = await _context.PromotionCampaigns.AnyAsync(x => x.Id == campaignId);
                if (!campaignExists)
                    throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi.");

                var targetVariantIds = dto.VariantIds ?? new List<int>();

                var toRemove = await _context.PromotionVariants
                    .Where(x => x.PromotionCampaignId == campaignId && targetVariantIds.Contains(x.VariantId))
                    .ToListAsync();

                if (toRemove.Any())
                {
                    _context.PromotionVariants.RemoveRange(toRemove);
                    await _context.SaveChangesAsync();
                }
                return true;
            });
        }

        #endregion
    }
}
