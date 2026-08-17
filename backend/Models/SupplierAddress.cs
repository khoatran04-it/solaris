namespace backend.Models
{
    /// <summary>
    /// Thực thể Địa chỉ kho / bến bãi lấy hàng của Nhà Cung Cấp.
    /// </summary>
    public class SupplierAddress : ISoftDelete
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Tên người liên hệ tại điểm giao nhận</summary>
        public required string ContactName { get; set; }

        /// <summary>Số điện thoại người liên hệ</summary>
        public required string ContactPhone { get; set; }

        // --- ADDRESS STRUCTURE ---
        public required string Province { get; set; }
        public required string District { get; set; }
        public required string Ward { get; set; }
        public required string StreetAddress { get; set; }

        /// <summary>Địa chỉ đầy đủ ghép từ các cấp hành chính</summary>
        public string FullAddress => $"{StreetAddress}, {Ward}, {District}, {Province}";

        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
