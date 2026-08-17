using AutoMapper;
using backend.Data;
using backend.DTOs;
using backend.DTOs.PromotionCampaignDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class PromotionCampaignService : IPromotionCampaignService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public PromotionCampaignService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PromotionCampaignReadDto>> GetAllListAsync(bool isActiveOnly = false)
        {
            var query = _context.PromotionCampaigns
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

        public async Task<PagedResult<PromotionCampaignReadDto>> GetPagedAsync(
            string? search, bool? isActive, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize)
        {
            var query = _context.PromotionCampaigns.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(lowerSearch));
            }

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // Lọc chiến dịch trong khoảng thời gian nhất định
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

        public async Task<PromotionCampaignReadDto> GetByIdAsync(int id)
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

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi.");

            return _mapper.Map<PromotionCampaignReadDto>(entity);
        }

        public async Task<int> CreateAsync(PromotionCampaignCreateDto dto)
        {
            var trimmedName = dto.Name.Trim();

            // 1. Validation Logic
            if (dto.StartDate >= dto.EndDate)
                throw new Exception("Ngày bắt đầu phải trước ngày kết thúc chiến dịch.");

            if (dto.IsPercentage && (dto.DiscountValue <= 0 || dto.DiscountValue > 100))
                throw new Exception("Mức giảm theo phần trăm phải nằm trong khoảng từ 0.01% đến 100%.");

            if (!dto.IsPercentage && dto.DiscountValue <= 0)
                throw new Exception("Mức giảm tiền mặt phải lớn hơn 0.");

            var isDuplicate = await _context.PromotionCampaigns
                .AnyAsync(x => x.Name.ToLower() == trimmedName.ToLower());
            if (isDuplicate)
                throw new Exception($"Chiến dịch khuyến mãi '{trimmedName}' đã tồn tại trong hệ thống.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = _mapper.Map<PromotionCampaign>(dto);
                entity.Name = trimmedName;
                entity.Description = dto.Description?.Trim();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.IsActive = dto.IsActive;

                _context.PromotionCampaigns.Add(entity);
                await _context.SaveChangesAsync(); // Lưu để lấy ID chiến dịch

                // Nếu có truyền danh sách biến thể ngay lúc tạo
                if (dto.VariantIds != null && dto.VariantIds.Any())
                {
                    // Lọc những VariantId hợp lệ và chưa bị xóa
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

                await transaction.CommitAsync();
                return entity.Id;
            }
            catch { await transaction.RollbackAsync(); throw; }
        }

        public async Task<bool> UpdateAsync(int id, PromotionCampaignUpdateDto dto)
        {
            var entity = await _context.PromotionCampaigns.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi cần sửa.");

            var trimmedName = dto.Name.Trim();

            // 1. Validation Logic
            if (dto.StartDate >= dto.EndDate)
                throw new Exception("Ngày bắt đầu phải trước ngày kết thúc chiến dịch.");

            if (dto.IsPercentage && (dto.DiscountValue <= 0 || dto.DiscountValue > 100))
                throw new Exception("Mức giảm theo phần trăm phải nằm trong khoảng từ 0.01% đến 100%.");

            if (!dto.IsPercentage && dto.DiscountValue <= 0)
                throw new Exception("Mức giảm tiền mặt phải lớn hơn 0.");

            var isDuplicate = await _context.PromotionCampaigns
                .AnyAsync(x => x.Id != id && x.Name.ToLower() == trimmedName.ToLower());
            if (isDuplicate)
                throw new Exception($"Cập nhật thất bại: Tên chiến dịch '{trimmedName}' đã bị trùng.");

            _mapper.Map(dto, entity);
            entity.Name = trimmedName;
            entity.Description = dto.Description?.Trim();
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddVariantsToCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto)
        {
            var campaignExists = await _context.PromotionCampaigns.AnyAsync(x => x.Id == campaignId);
            if (!campaignExists)
                throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi.");

            // Lấy danh sách các sản phẩm đang được áp dụng KM hiện tại trong DB
            var currentLinks = await _context.PromotionVariants
                .Where(x => x.PromotionCampaignId == campaignId)
                .ToListAsync();

            var currentVariantIds = currentLinks.Select(x => x.VariantId).ToList();

            // 1. Tìm những SP bị người dùng "Bỏ tích"
            var toRemove = currentLinks.Where(x => !dto.VariantIds.Contains(x.VariantId)).ToList();

            // 2. Tìm những SP được "Tích mới" (đảm bảo tồn tại và chưa bị xóa)
            var newVariantIds = dto.VariantIds.Where(vId => !currentVariantIds.Contains(vId)).Distinct().ToList();

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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (toRemove.Any()) _context.PromotionVariants.RemoveRange(toRemove);
                if (toAdd.Any()) _context.PromotionVariants.AddRange(toAdd);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return true;
        }

        public async Task<bool> RemoveVariantsFromCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto)
        {
            var toRemove = await _context.PromotionVariants
                .Where(x => x.PromotionCampaignId == campaignId && dto.VariantIds.Contains(x.VariantId))
                .ToListAsync();

            if (toRemove.Any())
            {
                _context.PromotionVariants.RemoveRange(toRemove);
                await _context.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.PromotionCampaigns
                .Include(x => x.PromotionVariants)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch để xóa.");

            // Soft delete chiến dịch
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            // Xóa sạch liên kết PromotionVariants khi chiến dịch bị xóa
            if (entity.PromotionVariants.Any())
            {
                _context.PromotionVariants.RemoveRange(entity.PromotionVariants);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.PromotionCampaigns.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch khuyến mãi.");

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
