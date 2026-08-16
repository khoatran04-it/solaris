namespace backend.Models
{
    public class InventoryReceiptDetail
    {
        public int Id { get; set; }

        // --- SỐ LIỆU KIỂM ĐẾM THỰC TẾ ---
        public decimal ExpectedQuantity { get; set; } // Số lượng dự kiến (từ PO hoặc tự nhập nếu không có PO)
        public decimal AcceptedQuantity { get; set; } // SL đạt chuẩn -> Cộng vào QuantityAvailable khi Completed
        public decimal RejectedQuantity { get; set; } // SL bị từ chối (dập nát, sai quy cách, hư hỏng)
        public string? RejectReason { get; set; } // Lý do từ chối (VD: Thối rữa, sai khối lượng)

        // --- NAVIGATION PROPERTIES ---
        public int InventoryReceiptId { get; set; }
        public virtual InventoryReceipt? InventoryReceipt { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // Trỏ về Lô hàng (ProductBatch) -> Biết nhập lô gì, truy vết NSX/HSD/NCC
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        // Đơn vị tính khi nhập kho (VD: Kg, Thùng, Khay)
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        // Liên kết ngược về dòng PO gốc (Nullable: nhập kho không qua PO thì để null)
        // Đặt ở Detail level để 1 Phiếu Nhập (IR) có thể chứa hàng từ nhiều PO khác nhau
        public int? PurchaseOrderDetailId { get; set; }
        public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
    }
}
