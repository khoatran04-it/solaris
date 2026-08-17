namespace backend.Models
{
    /// <summary>
    /// Thực thể Hạng / Bậc Khách Hàng (Customer Tier / Loyalty Level).
    /// Quy định mức chiết khấu tự động trên mỗi đơn hàng theo hạn mức chi tiêu tích lũy.
    /// </summary>
    public class CustomerTier : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã hạng (Ví dụ: BRONZE, SILVER, GOLD, DIAMOND)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị (Ví dụ: Thành Viên Đồng, Hạng Vàng, Kim Cương)</summary>
        public required string Name { get; set; }

        /// <summary>% Chiết khấu tự động trừ thẳng vào đơn hàng (Ví dụ: 5% cho Hạng Vàng)</summary>
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>Hạn mức tổng chi tiêu tối thiểu để đạt thứ hạng này (VND)</summary>
        public decimal MinSpending { get; set; } = 0;

        /// <summary>Trạng thái hoạt động (true: Hoạt động, false: Tạm khóa)</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}