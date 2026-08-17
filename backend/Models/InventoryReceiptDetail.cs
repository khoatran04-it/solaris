namespace backend.Models
{
    /// <summary>
    /// Dòng chi tiết kiểm đếm mặt hàng trong Phiếu Nhập Kho.
    /// </summary>
    public class InventoryReceiptDetail
    {
        public int Id { get; set; }

        // --- SỐ LIỆU KIỂM ĐẾM THỰC TẾ ---
        /// <summary>Số lượng dự kiến giao (từ đơn PO hoặc hóa đơn giao hàng của NCC)</summary>
        public decimal ExpectedQuantity { get; set; }

        /// <summary>Số lượng đạt chuẩn kiểm định -> Được cộng trực tiếp vào QuantityAvailable khi Hoàn tất</summary>
        public decimal AcceptedQuantity { get; set; }

        /// <summary>Số lượng bị từ chối / trả về ngay tại cửa kho (dập nát, thối rữa, sai quy cách)</summary>
        public decimal RejectedQuantity { get; set; }

        /// <summary>Lý do từ chối nhận hàng</summary>
        public string? RejectReason { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public int InventoryReceiptId { get; set; }
        public virtual InventoryReceipt? InventoryReceipt { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Lô hàng nông sản được tạo hoặc chọn để nhập</summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        /// <summary>Đơn vị tính khi nhập kho (Kg, Thùng, Hộp...)</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>ID dòng PO tham chiếu (nếu nhập hàng theo PO)</summary>
        public int? PurchaseOrderDetailId { get; set; }
        public virtual PurchaseOrderDetail? PurchaseOrderDetail { get; set; }
    }
}
