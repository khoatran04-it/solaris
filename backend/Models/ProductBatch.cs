namespace backend.Models
{
    public class ProductBatch : ISoftDelete
    {
        public int Id { get; set; }

        public required string BatchCode { get; set; } // VD: BATCH-040826-CACHUA

        // 🔥 KỶ LUẬT THÉP: Nông sản bắt buộc phải có Ngày SX và HSD (Bỏ dấu ?)
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Mỗi lô chỉ đại diện cho 1 Mã hàng cụ thể (Để tính Date chính xác)
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // 🔥 KỶ LUẬT THÉP: Ép buộc phải biết nhập từ ai để truy vết ngộ độc/chất lượng (Bỏ dấu ?)
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}