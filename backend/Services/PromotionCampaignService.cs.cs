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

        public async Task<IEnumerable<PromotionCampaignReadDto>> GetAllListAsync()
        {
            var items = await _context.PromotionCampaigns
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<PromotionCampaignReadDto>>(items);
        }

        public async Task<PagedResult<PromotionCampaignReadDto>> GetPagedAsync(
            string? search, bool? isActive, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize)
        {
            var query = _context.PromotionCampaigns.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // Lọc chiến dịch trong khoảng thời gian nhất định
            if (startDate.HasValue) query = query.Where(x => x.StartDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(x => x.EndDate <= endDate.Value);

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
                // 🔥 Nhánh 2 (Bắt đầu lại từ rễ): Kéo Variant -> Prices -> UoM
                .Include(x => x.PromotionVariants)
                    .ThenInclude(pv => pv.Variant)
                        .ThenInclude(v => v!.Prices)
                            .ThenInclude(p => p.UoM)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch.");

            return _mapper.Map<PromotionCampaignReadDto>(entity);
        }

        public async Task<int> CreateAsync(PromotionCampaignCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = _mapper.Map<PromotionCampaign>(dto);
                _context.PromotionCampaigns.Add(entity);
                await _context.SaveChangesAsync(); // Lưu để lấy ID chiến dịch

                // Nếu có truyền danh sách biến thể ngay lúc tạo
                if (dto.VariantIds.Any())
                {
                    var links = dto.VariantIds.Select(vId => new PromotionVariant
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
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // 🔥 THAY ĐỔI CỰC KỲ QUAN TRỌNG: Sửa lại logic thành Sync (Đồng bộ)
        public async Task<bool> AddVariantsToCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto)
        {
            // Lấy danh sách các sản phẩm đang được áp dụng KM hiện tại trong DB
            var currentLinks = await _context.PromotionVariants
                .Where(x => x.PromotionCampaignId == campaignId)
                .ToListAsync();

            var currentVariantIds = currentLinks.Select(x => x.VariantId).ToList();

            // 1. Tìm những SP bị người dùng "Bỏ tích" (Có trong DB nhưng không có trong mảng Frontend gửi lên)
            var toRemove = currentLinks.Where(x => !dto.VariantIds.Contains(x.VariantId)).ToList();

            // 2. Tìm những SP được "Tích mới" (Gửi lên nhưng chưa có trong DB)
            var toAddIds = dto.VariantIds.Where(vId => !currentVariantIds.Contains(vId)).ToList();
            var toAdd = toAddIds.Select(vId => new PromotionVariant
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
            var entity = await _context.PromotionCampaigns.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch.");
            _context.PromotionCampaigns.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var entity = await _context.PromotionCampaigns.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy chiến dịch.");
            entity.IsActive = !entity.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}