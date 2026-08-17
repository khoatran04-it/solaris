namespace backend.Models
{
    public class PromotionVariant
    {
        public int Id { get; set; }

        // Trỏ về Chiến dịch
        public int PromotionCampaignId { get; set; }
        public virtual PromotionCampaign? PromotionCampaign { get; set; }

        // Trỏ về Biến thể sản phẩm
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // Audit cơ bản cho bảng nối (Biết lúc nào add biến thể này vào campaign)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}