using System;

namespace backend.DTOs.CustomerTierDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Hạng / Bậc Khách Hàng.
    /// Dùng để trả về cho Front-end (Web/App) hoặc trang Quản trị.
    /// </summary>
    public class CustomerTierReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã hạng thành viên.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị của hạng.</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Cấu hình Hạng & Chiết khấu
        /// <summary>Phần trăm chiết khấu tự động.</summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>Hạn mức tổng chi tiêu tích lũy tối thiểu (VNĐ).</summary>
        public decimal MinSpending { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động.</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}