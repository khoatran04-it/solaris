namespace backend.Models
{
    /// <summary>
    /// Thực thể Bảng Giá Bán Của Biến Thể Theo Từng Đơn Vị Tính.
    /// Cho phép 1 SKU có nhiều mức giá tương ứng với từng ĐVT (Ví dụ: Giá bán theo Kg, Giá bán theo Thùng, Giá bán theo Hộp).
    /// </summary>
    public class ProductVariantPrice : ISoftDelete
    {
        public int Id { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Đơn vị tính bán hàng</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Đơn giá niêm yết bán hàng (VND)</summary>
        public decimal Price { get; set; }

        /// <summary>True = Là ĐVT bán hàng mặc định khi tạo đơn</summary>
        public bool IsDefault { get; set; } = false;

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}