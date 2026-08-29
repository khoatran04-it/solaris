namespace backend.Models
{
    /// <summary>
    /// Thực thể Địa Chỉ Vật Lý Của Kho Hàng (Warehouse Address).
    /// Lưu trữ vị trí hành chính chi tiết và Tọa độ GPS (Latitude, Longitude) phục vụ thuật toán 
    /// định tuyến đơn hàng tự động (Smart Routing) và tính toán chi phí vận chuyển.
    /// </summary>
    public class WarehouseAddress : ISoftDelete
    {
        public int Id { get; set; }

        #region Cấu trúc Hành chính (Administrative Unit)
        /// <summary>Tỉnh / Thành phố trực thuộc trung ương.</summary>
        public required string Province { get; set; }

        /// <summary>Quận / Huyện / Thị xã / Thành phố trực thuộc tỉnh.</summary>
        public required string District { get; set; }

        /// <summary>Phường / Xã / Thị trấn.</summary>
        public required string Ward { get; set; }

        /// <summary>Số nhà, Tên đường, Tòa nhà hoặc Khu công nghiệp.</summary>
        public required string StreetAddress { get; set; }

        /// <summary>
        /// Địa chỉ đầy đủ, tự động ghép nối từ các cấp hành chính nhỏ đến lớn.
        /// Đây là thuộc tính tính toán (Computed Property), thường không lưu trực tiếp vào CSDL mà 
        /// dùng để trả về cho Client hiển thị trên giao diện hoặc in ấn phiếu giao hàng.
        /// </summary>
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";
        #endregion

        #region Định vị Không gian (Geolocation / GPS)
        /// <summary>Vĩ độ GPS (Latitude) - Hỗ trợ ghim vị trí kho trên bản đồ số.</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ GPS (Longitude) - Kết hợp cùng Vĩ độ để tính toán khoảng cách đường chim bay tới địa chỉ khách hàng.</summary>
        public double Longitude { get; set; }
        #endregion

        #region Hệ thống
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion
    }
}