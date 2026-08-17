namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhóm Ngành Hàng Lớn (Product Category Group).
    /// Ví dụ: Nông sản tươi, Thực phẩm khô, Đồ uống đóng chai, Thủy hải sản.
    /// </summary>
    public class ProductCategoryGroup : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã nhóm ngành hàng (Ví dụ: FRESH_PRODUCE, DRY_FOOD)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị nhóm ngành hàng</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả chi tiết</summary>
        public string? Description { get; set; }

        /// <summary>Ảnh đại diện / Banner nhóm ngành</summary>
        public string? ImagePath { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
    }
}
