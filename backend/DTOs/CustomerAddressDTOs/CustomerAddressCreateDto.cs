namespace backend.DTOs.CustomerAddressDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Địa chỉ giao hàng của Khách hàng.
    /// Có thể đính kèm tọa độ GPS để hỗ trợ thuật toán chọn kho giao hàng gần nhất.
    /// </summary>
    public class CustomerAddressCreateDto
    {
        #region Liên kết đối tượng
        /// <summary>Mã định danh của khách hàng sở hữu địa chỉ này.</summary>
        public int CustomerId { get; set; }
        #endregion

        #region Thông tin Người nhận
        /// <summary>Tên người nhận hàng thực tế tại địa chỉ này.</summary>
        public string ReceiverName { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên hệ cho Shipper (Có thể khác số điện thoại đăng nhập của khách).</summary>
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
        public bool IsDefault { get; set; } = false;
        #endregion
    }
}