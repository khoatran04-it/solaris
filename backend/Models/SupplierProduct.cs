namespace backend.Models
{
    public class SupplierProduct : ISoftDelete
    {
        public int Id { get; set; }

        // Mã sản phẩm theo cách gọi của Nhà Cung Cấp (Rất quan trọng để in Đơn Đặt Hàng không bị nhầm)
        public string? SupplierSKU { get; set; }

        // Giá nhập mặc định (theo PurchaseUoM) - Auto-fill vào PO, cho phép NV sửa nếu giá biến động
        public decimal LastImportPrice { get; set; }

        // Số lượng đặt hàng tối thiểu (MOQ) - Dùng decimal cho tương thích Base UoM (VD: 0.5 tấn)
        public decimal MinimumOrderQuantity { get; set; } = 1;

        // Thời gian giao hàng dự kiến tính bằng ngày (Lead Time) - Để app tự nhắc ngày đặt hàng
        public int LeadTimeDays { get; set; } = 0;

        // --- AUDIT FIELDS ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        // 1. Thuộc về Biến thể sản phẩm nào của hệ thống mình?
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // 2. Của Nhà cung cấp nào?
        public int SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        // 3. Đơn vị tính mua hàng (VD: NCC bán theo Thùng, nhưng Base UoM của hệ thống là Kg)
        public int PurchaseUoMId { get; set; }
        public virtual UoM? PurchaseUoM { get; set; }
    }
}