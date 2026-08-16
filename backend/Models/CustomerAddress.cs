namespace backend.Models
{
    public class CustomerAddress : ISoftDelete
    {
        public int Id { get; set; }
        public required string ReceiverName { get; set; }
        public required string Phone { get; set; }

        // --- ADDRESS STRUCTURE ---
        public required string Province { get; set; } // Tỉnh/Thành phố
        public required string District { get; set; } // Quận/Huyện
        public required string Ward { get; set; } // Phường/Xã
        public required string StreetAddress { get; set; } // Số nhà, tên đường, ngõ ngách

        // Cột bổ sung để hiển thị nhanh trên UI (EF Core sẽ tự hiểu đây là cột tính toán và không map vào DB)
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- FOREIGN KEY ---
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}