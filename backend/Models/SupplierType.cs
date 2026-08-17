namespace backend.Models
{
    /// <summary>
    /// Thực thể Loại Nhà Cung Cấp (Ví dụ: Nông hộ trực tiếp, Hợp tác xã, Tổng kho phân phối, Nhà máy chế biến).
    /// </summary>
    public class SupplierType : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã loại nhà cung cấp (Ví dụ: FARM, DISTRIBUTOR, FACTORY)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị phân loại</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả chi tiết</summary>
        public string? Description { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Trạng thái hoạt động (true: Hoạt động, false: Tạm khóa)</summary>
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    }
}