namespace backend.Models
{
    /// <summary>
    /// Thực thể Hạng / Bậc Khách Hàng (Customer Tier / Loyalty Level).
    /// Quản lý chính sách thẻ thành viên, quy định mức chiết khấu tự động và điều kiện nâng hạng dựa trên hạn mức chi tiêu tích lũy.
    /// </summary>
    public class CustomerTier : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã hạng thành viên (Ví dụ: BRONZE, SILVER, GOLD, DIAMOND).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của hạng (Ví dụ: Thành Viên Đồng, Hạng Vàng, Kim Cương).</summary>
        public required string Name { get; set; }
        #endregion

        #region Cấu hình Hạng & Chiết khấu
        /// <summary>Phần trăm chiết khấu tự động được áp dụng trực tiếp vào tổng đơn hàng cho khách thuộc hạng này (Ví dụ: 5.0 cho 5%).</summary>
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>Hạn mức tổng chi tiêu tích lũy tối thiểu (VNĐ) để khách hàng đạt được mức hạng này.</summary>
        public decimal MinSpending { get; set; } = 0;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang áp dụng chính sách hạng này, false: Tạm ngưng áp dụng).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Danh sách các khách hàng hiện đang đạt mức thứ hạng này.</summary>
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
        #endregion
    }
}