namespace backend.Models
{
    /// <summary>
    /// Thực thể trung gian (Join Entity) kết nối Chiến dịch khuyến mãi và Biến thể sản phẩm.
    /// Quản lý danh sách các mặt hàng (SKU) cụ thể được áp dụng mức giảm giá trong một chiến dịch.
    /// </summary>
    public class PromotionVariant
    {
        public int Id { get; set; }

        #region Liên kết Chiến dịch Khuyến mãi
        /// <summary>Mã định danh của Chiến dịch khuyến mãi.</summary>
        public int PromotionCampaignId { get; set; }

        /// <summary>Thực thể Chiến dịch khuyến mãi chủ quản.</summary>
        public virtual PromotionCampaign? PromotionCampaign { get; set; }
        #endregion

        #region Liên kết Biến thể Sản phẩm (SKU)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) được áp dụng mức giảm giá.</summary>
        public int VariantId { get; set; }

        /// <summary>Thực thể Biến thể sản phẩm.</summary>
        public virtual ProductVariant? Variant { get; set; }
        #endregion

        #region Hệ thống & Truy vết (Audit)
        /// <summary>Thời điểm biến thể này được thêm vào chiến dịch (Hỗ trợ truy vết xem sản phẩm được đưa vào chương trình sale lúc nào).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
}