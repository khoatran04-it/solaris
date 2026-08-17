namespace backend.Models
{
    /// <summary>
    /// Thực thể Biến Thể Sản Phẩm (SKU - Stock Keeping Unit).
    /// Đây là đơn vị hàng hóa vật lý cụ thể được nhập kho, lưu kho, xuất bán và quản lý tồn kho.
    /// Ví dụ: Táo Envy Size Nhỏ 1kg, Táo Envy Size Lớn 1kg, Táo Envy Thùng 10kg.
    /// </summary>
    public class ProductVariant : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã SKU biến thể (Ví dụ: SKU-ENVY-S-1KG, SKU-ENVY-BOX-10KG)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị biến thể</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả quy cách biến thể</summary>
        public string? Description { get; set; }

        /// <summary>Ảnh chi tiết của biến thể</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mức tồn kho an toàn tối thiểu (Safety Stock - tự động cảnh báo khi tồn kho xuống dưới mức này)</summary>
        public int InventoryGuideline { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        /// <summary>Danh sách thuộc tính EAV động của biến thể (Brix, Nguồn gốc, Chuẩn hữu cơ...)</summary>
        public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();

        /// <summary>Danh sách các Lô hàng nông sản đã nhập của biến thể này</summary>
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();

        /// <summary>Danh sách Nhà cung cấp cung ứng biến thể này</summary>
        public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

        /// <summary>Các chương trình khuyến mãi đang áp dụng cho biến thể này</summary>
        public virtual ICollection<PromotionVariant> PromotionVariants { get; set; } = new List<PromotionVariant>();

        /// <summary>Bảng giá bán theo các Đơn vị tính khác nhau (Bán lẻ theo Kg, Bán sỉ theo Thùng...)</summary>
        public virtual ICollection<ProductVariantPrice> Prices { get; set; } = new List<ProductVariantPrice>();
    }
}