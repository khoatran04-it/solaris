namespace backend.Models
{
    /// <summary>
    /// Thực thể Bảng Giá & Danh Mục Mặt Hàng Của Nhà Cung Cấp.
    /// Xác định đơn giá nhập mặc định, số lượng đặt tối thiểu (MOQ) và thời gian giao hàng (Lead Time).
    /// </summary>
    public class SupplierProduct : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã SKU nội bộ của Nhà Cung Cấp</summary>
        public string? SupplierSKU { get; set; }

        /// <summary>Đơn giá nhập tham chiếu gần nhất</summary>
        public decimal LastImportPrice { get; set; }

        /// <summary>Số lượng đặt hàng tối thiểu (MOQ - Minimum Order Quantity)</summary>
        public decimal MinimumOrderQuantity { get; set; } = 1;

        /// <summary>Thời gian chuẩn bị và giao hàng tính bằng ngày (Lead Time Days)</summary>
        public int LeadTimeDays { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>ID Biến thể sản phẩm trong hệ thống</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>ID Nhà cung cấp</summary>
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Đơn vị tính mua hàng từ NCC (Ví dụ: Thùng, Bao, Két)</summary>
        public int PurchaseUoMId { get; set; }
        public virtual UoM? PurchaseUoM { get; set; }
    }
}