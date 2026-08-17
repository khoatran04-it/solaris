using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.PromotionCampaignDTOs
{
    public class PromotionCampaignUpdateDto
    {
        [Required(ErrorMessage = "Tên chiến dịch không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên chiến dịch tối đa 255 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        public bool IsPercentage { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá không được để trống.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Mức giảm giá phải lớn hơn 0.")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
