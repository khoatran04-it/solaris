namespace backend.DTOs.PromotionCampaignDTOs
{
    public class PromotionCampaignReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsPercentage { get; set; }
        public decimal DiscountValue { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<CampaignAppliedVariantDto> AppliedVariants { get; set; } = new();
    }

    public class CampaignAppliedVariantDto
    {
        public int VariantId { get; set; }
        public string VariantCode { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? ImagePath { get; set; }
        public decimal DefaultPrice { get; set; }
        public string? DefaultUoMName { get; set; }
    }
}