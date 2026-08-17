namespace backend.Models
{
    /// <summary>
    /// Thực thể Địa Chỉ Vật Lý Của Kho Hàng.
    /// Lưu trữ vị trí hành chính và Tọa độ GPS (Latitude, Longitude) phục vụ Thuật toán Định tuyến đơn hàng (Smart Routing).
    /// </summary>
    public class WarehouseAddress : ISoftDelete
    {
        public int Id { get; set; }

        // --- ADDRESS STRUCTURE ---
        public required string Province { get; set; }
        public required string District { get; set; }
        public required string Ward { get; set; }
        public required string StreetAddress { get; set; }

        /// <summary>Địa chỉ đầy đủ ghép từ các cấp hành chính</summary>
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Vĩ độ GPS (Latitude)</summary>
        public double Latitude { get; set; }

        /// <summary>Kinh độ GPS (Longitude)</summary>
        public double Longitude { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}