namespace backend.Models
{
    /// <summary>
    /// Dòng chi tiết mặt hàng và Lô hàng trong Phiếu Xuất Kho.
    /// </summary>
    public class InventoryIssueDetail
    {
        public int Id { get; set; }

        public int InventoryIssueId { get; set; }
        public virtual InventoryIssue? InventoryIssue { get; set; }

        public int? OrderDetailId { get; set; }
        public virtual OrderDetail? OrderDetail { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>KỶ LUẬT THÉP: Mọi dòng xuất kho phải chỉ đích danh Lô hàng (Batch) theo FEFO</summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Số lượng xuất theo ĐVT</summary>
        public decimal Quantity { get; set; }

        /// <summary>Đơn giá xuất (VND)</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Tổng tiền = Quantity * UnitPrice</summary>
        public decimal TotalPrice { get; set; }
    }
}
