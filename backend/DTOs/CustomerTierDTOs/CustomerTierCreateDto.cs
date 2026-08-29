namespace backend.DTOs.CustomerTierDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Hạng / Bậc Khách Hàng (Customer Tier / Loyalty Level).
    /// Thiết lập chính sách nâng hạng và chiết khấu tự động cho khách hàng.
    /// </summary>
    public class CustomerTierCreateDto
    {
        #region Thông tin Định danh
        /// <summary>Mã hạng thành viên (Ví dụ: BRONZE, SILVER, GOLD, DIAMOND).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của hạng (Ví dụ: Thành Viên Đồng, Hạng Vàng, Kim Cương).</summary>
        public string Name { get; set; } = string.Empty;
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
        #endregion
    }
}