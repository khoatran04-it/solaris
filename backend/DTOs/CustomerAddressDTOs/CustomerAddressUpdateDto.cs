namespace backend.DTOs.CustomerAddressDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Địa chỉ giao hàng.
    /// Lưu ý: Không cho phép cập nhật CustomerId vì địa chỉ thuộc về một khách hàng cố định.
    /// </summary>
    public class CustomerAddressUpdateDto
    {
        #region Thông tin Người nhận
        /// <summary>Tên người nhận hàng thực tế tại địa chỉ này.</summary>
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên hệ cho Shipper.</summary>
        public string Phone { get; set; } = string.Empty;
        #endregion

        #region Cấu trúc Hành chính & Địa chỉ
        /// <summary>Tỉnh / Thành phố.</summary>
        public string Province { get; set; } = string.Empty;

        /// <summary>Quận / Huyện.</summary>
        public string District { get; set; } = string.Empty;

        /// <summary>Phường / Xã.</summary>
        public string Ward { get; set; } = string.Empty;

        /// <summary>Địa chỉ chi tiết (Số nhà, Tên đường, Tòa nhà...).</summary>
        public string StreetAddress { get; set; } = string.Empty;
        #endregion

        #region Tọa độ Địa lý (Smart Order Routing)
        /// <summary>Vĩ độ GPS (Latitude) trên bản đồ.</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ GPS (Longitude) trên bản đồ.</summary>
        public double Longitude { get; set; }
        #endregion

        #region Trạng thái
        /// <summary>Cờ đánh dấu thiết lập làm địa chỉ mặc định khi thanh toán đơn hàng.</summary>
        public bool IsDefault { get; set; }
        #endregion
    }
}