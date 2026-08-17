namespace backend.Models
{
    /// <summary>
    /// Thực thể Lô Hàng Nông Sản (Product Batch / Lot).
    /// Kỷ luật thép của ngành thực phẩm & nông sản: Bắt buộc truy xuất nguồn gốc, Ngày sản xuất (NSX) và Hạn sử dụng (HSD).
    /// </summary>
    public class ProductBatch : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã lô duy nhất (Ví dụ: BATCH-20260817-CACHUA-01)</summary>
        public required string BatchCode { get; set; }

        /// <summary>Ngày sản xuất / Ngày thu hoạch</summary>
        public DateTime ManufactureDate { get; set; }

        /// <summary>Hạn sử dụng (Dùng cho thuật toán FEFO - Hết hạn trước xuất trước)</summary>
        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        /// <summary>Thuộc về Biến thể sản phẩm cụ thể nào</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Nhà cung cấp / Nông hộ cung ứng lô hàng này (phục vụ truy vết an toàn thực phẩm)</summary>
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}