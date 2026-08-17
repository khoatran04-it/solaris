namespace backend.Models
{
    /// <summary>
    /// Thực thể Sổ Địa Chỉ Giao Hàng của Khách Hàng.
    /// Tích hợp Tọa độ Địa lý (Latitude, Longitude) phục vụ Thuật toán Định tuyến Kho thông minh (Smart Order Routing).
    /// </summary>
    public class CustomerAddress : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Tên người nhận hàng tại địa chỉ này</summary>
        public required string ReceiverName { get; set; }

        /// <summary>Số điện thoại liên hệ nhận hàng</summary>
        public required string Phone { get; set; }

        // --- ADDRESS STRUCTURE ---
        public required string Province { get; set; }
        public required string District { get; set; }
        public required string Ward { get; set; }
        public required string StreetAddress { get; set; }

        /// <summary>Địa chỉ đầy đủ ghép từ các cấp hành chính</summary>
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";

        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Vĩ độ GPS (Latitude) phục vụ tính khoảng cách Haversine đến Kho hàng</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ GPS (Longitude) phục vụ tính khoảng cách Haversine đến Kho hàng</summary>
        public double Longitude { get; set; }

        // --- FOREIGN KEY ---
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}