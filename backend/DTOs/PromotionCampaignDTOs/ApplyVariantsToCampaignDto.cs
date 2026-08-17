using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.PromotionCampaignDTOs
{
    public class ApplyVariantsToCampaignDto
    {
        [Required(ErrorMessage = "Danh sách biến thể không được để trống.")]
        public List<int> VariantIds { get; set; } = new();
    }
}
