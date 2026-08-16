using backend.DTOs;
using backend.DTOs.PromotionCampaignDTOs;

namespace backend.Services.Interfaces
{
    public interface IPromotionCampaignService
    {
        // --- CÁC HÀM CRUD CƠ BẢN CHO VỎ CHIẾN DỊCH ---

        Task<IEnumerable<PromotionCampaignReadDto>> GetAllListAsync();

        Task<PagedResult<PromotionCampaignReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize
        );

        Task<PromotionCampaignReadDto> GetByIdAsync(int id);

        Task<int> CreateAsync(PromotionCampaignCreateDto dto);

        Task<bool> UpdateAsync(int id, PromotionCampaignUpdateDto dto);

        Task<bool> DeleteAsync(int id);

        Task<bool> ToggleActiveAsync(int id);

        // --- CÁC HÀM ACTION XỬ LÝ BẢNG TRUNG GIAN (N-N) ---

        /// <summary>
        /// Gắn thêm một danh sách biến thể vào chiến dịch (Multi-select)
        /// </summary>
        Task<bool> AddVariantsToCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto);

        /// <summary>
        /// Gỡ một danh sách biến thể khỏi chiến dịch (Multi-select)
        /// </summary>
        Task<bool> RemoveVariantsFromCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto);
    }
}