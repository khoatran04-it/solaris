namespace backend.Models
{
    /// <summary>
    /// Dòng chi tiết mặt hàng đặt mua trong Đơn Mua Hàng (PO Detail).
    /// </summary>
    public class PurchaseOrderDetail
    {
        public int Id { get; set; }

        /// <summary>Số lượng đặt mua (theo Đơn vị tính UoM)</summary>
        public decimal OrderQuantity { get; set; }

        /// <summary>Đơn giá đặt mua (VND)</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Thành tiền = OrderQuantity * UnitPrice</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>Số lượng thực tế đã nhập kho lũy kế (hỗ trợ nhập hàng nhiều đợt - Partial Receipt)</summary>
        public decimal ReceivedQuantity { get; set; } = 0;

        // --- NAVIGATION PROPERTIES ---
        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Đơn vị tính khi đặt mua từ NCC (Thùng, Bao, Két...)</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Danh sách các đợt nhập kho kiểm đếm tương ứng với dòng PO này</summary>
        public virtual ICollection<InventoryReceiptDetail> ReceiptDetails { get; set; } = new List<InventoryReceiptDetail>();
    }
}
