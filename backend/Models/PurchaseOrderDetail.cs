namespace backend.Models
{
    public class PurchaseOrderDetail
    {
        public int Id { get; set; }

        public decimal OrderQuantity { get; set; } // Số lượng đặt (theo UoM bên dưới)
        public decimal UnitPrice { get; set; } // Đơn giá lúc đặt (Auto-fill từ SupplierProduct, cho phép Override)
        public decimal TotalPrice { get; set; } // = OrderQuantity * UnitPrice

        // Bài toán "Giao hàng từng đợt" (Partial Receipt):
        // Cột này được Service cộng dồn mỗi khi 1 InventoryReceiptDetail được Completed
        public decimal ReceivedQuantity { get; set; } = 0;

        // --- NAVIGATION PROPERTIES ---
        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // Đơn vị tính khi đặt hàng (Auto-fill từ SupplierProduct.PurchaseUoMId)
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        // Navigation ngược: Biết dòng PO này đã được nhập bởi những dòng Receipt Detail nào
        public virtual ICollection<InventoryReceiptDetail> ReceiptDetails { get; set; } = new List<InventoryReceiptDetail>();
    }
}
