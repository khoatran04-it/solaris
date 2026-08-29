using System;

namespace backend.DTOs.CustomerAddressDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Sổ địa chỉ giao hàng.
    /// Dùng để hiển thị danh sách địa chỉ trong mục Tài khoản (Web/App) hoặc bước Checkout.
    /// </summary>
    public class CustomerAddressReadDto
    {
        public int Id { get; set; }

        #region Liên kết đối tượng
        /// <summary>Mã định danh của Khách hàng.</summary>
        public int CustomerId { get; set; }
        #endregion

        #region Thông tin Người nhận
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        #endregion

        #region Cấu trúc Hành chính & Địa chỉ
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;

        /// <summary>Địa chỉ đầy đủ (Đã được nối chuỗi sẵn từ Tỉnh/Quận/Phường/Đường) để frontend hiển thị trực tiếp.</summary>
        public string FullAddress { get; set; } = string.Empty;
        #endregion

        #region Tọa độ Địa lý
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}