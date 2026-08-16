namespace backend.Models
{
    public class WarehouseAddress : ISoftDelete
    {
        public int Id { get; set; }

        // --- ADDRESS STRUCTURE ---
        public required string Province { get; set; }
        public required string District { get; set; }
        public required string Ward { get; set; }
        public required string StreetAddress { get; set; }

        // Cột bổ sung để hiển thị nhanh trên UI
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}