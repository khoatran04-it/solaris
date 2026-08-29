namespace backend.Models
{
    /// <summary>
    /// Thực thể Sổ Địa Chỉ Giao Hàng của Khách Hàng.
    /// Cho phép mỗi khách hàng lưu trữ nhiều địa chỉ nhận hàng khác nhau (Nhà riêng, Cơ quan...).
    /// Tích hợp sẵn Tọa độ Địa lý (Lat, Long) để phục vụ Thuật toán Định tuyến Kho thông minh (Smart Order Routing).
    /// </summary>
    public class CustomerAddress : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Người nhận
        /// <summary>Tên người nhận hàng tại địa chỉ này (Có thể khác với tên tài khoản khách hàng).</summary>
        public required string ReceiverName { get; set; }

        /// <summary>Số điện thoại liên hệ để Shipper giao hàng.</summary>
        public required string Phone { get; set; }
        #endregion

        #region Cấu trúc Hành chính & Địa chỉ
        /// <summary>Tỉnh / Thành phố.</summary>
        public required string Province { get; set; }

        /// <summary>Quận / Huyện.</summary>
        public required string District { get; set; }

        /// <summary>Phường / Xã.</summary>
        public required string Ward { get; set; }

        /// <summary>Địa chỉ chi tiết (Số nhà, Tên đường, Tòa nhà...).</summary>
        public required string StreetAddress { get; set; }

        /// <summary>
        /// Thuộc tính tính toán (Computed Property): Địa chỉ đầy đủ ghép từ các cấp hành chính.
        /// Dùng để hiển thị nhanh trên UI hoặc in lên Phiếu giao hàng.
        /// </summary>
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";
        #endregion

        #region Tọa độ Địa lý (Smart Order Routing)
        /// <summary>Vĩ độ GPS (Latitude). Phục vụ tính toán khoảng cách Haversine từ địa chỉ này đến các Kho hàng.</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ GPS (Longitude). Phục vụ định tuyến tự động chốt đơn cho kho gần nhất để tối ưu chi phí vận chuyển.</summary>
        public double Longitude { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Cờ đánh dấu đây là địa chỉ giao hàng mặc định được chọn sẵn khi khách hàng vào trang Checkout.</summary>
        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh của Khách hàng sở hữu địa chỉ này.</summary>
        public int CustomerId { get; set; }

        /// <summary>Thực thể Khách hàng chủ quản.</summary>
        public virtual Customer? Customer { get; set; }
        #endregion
    }
}