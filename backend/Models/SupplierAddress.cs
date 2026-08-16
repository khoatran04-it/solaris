namespace backend.Models
{
    public class SupplierAddress : ISoftDelete
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public required string ContactName { get; set; }
        public required string ContactPhone { get; set; }

        // --- ADDRESS STRUCTURE ---
        public required string Province { get; set; } // Tỉnh/Thành phố
        public required string District { get; set; } // Quận/Huyện
        public required string Ward { get; set; } // Phường/Xã
        public required string StreetAddress { get; set; } // Số nhà, tên đường, ngõ ngách
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";

        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
}
