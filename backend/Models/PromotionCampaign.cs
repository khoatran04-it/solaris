namespace backend.Models
{
    /// <summary>
    /// Thực thể Chiến Dịch Khuyến Mãi / Giảm Giá (Promotion Campaign).
    /// Hỗ trợ giảm giá theo % hoặc giảm giá tiền mặt cố định, áp dụng theo khoảng thời gian có hiệu lực.
    /// </summary>
    public class PromotionCampaign : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Tên chiến dịch khuyến mãi (Ví dụ: Flash Sale Cuối Tuần, Giảm Giá Mùa Thu Hoạch)</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn thân thiện cho SEO (Ví dụ: flash-sale-cuoi-tuan)</summary>
        public string? Slug { get; set; }

        /// <summary>Ảnh banner quảng cáo trên trang chủ / trang khuyến mãi</summary>
        public string? BannerImagePath { get; set; }

        /// <summary>Mô tả chi tiết và thể lệ chương trình</summary>
        public string? Description { get; set; }

        // --- LOGIC GIẢM GIÁ ---
        /// <summary>True = Giảm theo phần trăm (%), False = Giảm trừ tiền mặt cố định (VND)</summary>
        public bool IsPercentage { get; set; }

        /// <summary>Giá trị giảm (Ví dụ: 20 nếu là 20%, hoặc 50000 nếu là 50.000đ)</summary>
        public decimal DiscountValue { get; set; }

        // --- VÒNG ĐỜI CHIẾN DỊCH ---
        /// <summary>Thời điểm bắt đầu áp dụng khuyến mãi</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Thời điểm kết thúc chiến dịch</summary>
        public DateTime EndDate { get; set; }

        // --- AUDIT FIELDS & SOFT DELETE ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION ---
        /// <summary>Danh sách các biến thể SKU được áp dụng trong chiến dịch này</summary>
        public virtual ICollection<PromotionVariant> PromotionVariants { get; set; } = new List<PromotionVariant>();
    }
}