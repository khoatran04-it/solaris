namespace backend.Models
{
    /// <summary>
    /// Thực thể Sản Phẩm Cha (Product Master).
    /// Đại diện cho dòng sản phẩm chung (Ví dụ: Táo Envy New Zealand, Cà Chua Beef Đà Lạt, Gạo ST25).
    /// </summary>
    public class Product : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã sản phẩm chung (Ví dụ: PROD-APPLE-ENVY)</summary>
        public required string Code { get; set; }

        /// <summary>Tên dòng sản phẩm</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả tổng quan sản phẩm</summary>
        public string? Description { get; set; }

        /// <summary>Ảnh đại diện dòng sản phẩm</summary>
        public string? ImagePath { get; set; }

        // --- AUDIT FIELDS ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>Thuộc danh mục nào</summary>
        public int? CategoryId { get; set; }
        public virtual ProductCategory? Category { get; set; }

        /// <summary>Đơn vị tính cơ sở chuẩn của sản phẩm (Base UoM - Ví dụ: Kg, Quả, Cái)</summary>
        public int BaseUoMId { get; set; }
        public virtual UoM? BaseUoM { get; set; }

        /// <summary>Danh sách các SKU / Biến thể đóng gói bán hàng</summary>
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
