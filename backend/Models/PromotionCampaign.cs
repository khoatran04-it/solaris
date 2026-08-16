namespace backend.Models
{
    public class PromotionCampaign : ISoftDelete
    {
        public int Id { get; set; }
        public required string Name { get; set; } // Tên chiến dịch (VD: Flash Sale Cuối Tuần)
        public string? Description { get; set; }  // Ghi chú nội bộ

        // --- LOGIC GIẢM GIÁ ---
        public bool IsPercentage { get; set; }    // True: Giảm theo %, False: Giảm tiền mặt
        public decimal DiscountValue { get; set; } // Trị giá giảm (VD: 20% hoặc 50.000đ)

        // --- VÒNG ĐỜI CHIẾN DỊCH ---
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // --- AUDIT FIELDS & SOFT DELETE ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION ---
        // 1 Chiến dịch có thể áp dụng cho N Biến thể
        public virtual ICollection<PromotionVariant> PromotionVariants { get; set; } = new List<PromotionVariant>();
    }
}