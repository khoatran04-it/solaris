namespace backend.Models
{
    public class ProductVariant : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        public int InventoryGuideline { get; set; } // Mức tồn kho an toàn (cảnh báo hết hàng)

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
        public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
        public virtual ICollection<PromotionVariant> PromotionVariants { get; set; } = new List<PromotionVariant>();

        // 🔥 THÊM MỚI: Danh sách Quy cách bán & Bảng giá theo Đơn vị tính (UoM)
        public virtual ICollection<ProductVariantPrice> Prices { get; set; } = new List<ProductVariantPrice>();
    }
}