namespace backend.Models
{
    /// <summary>
    /// Dòng chi tiết mặt hàng và Lô hàng trong Phiếu điều chuyển liên kho.
    /// </summary>
    public class InventoryTransferDetail
    {
        public int Id { get; set; }

        public int InventoryTransferId { get; set; }
        public virtual InventoryTransfer? InventoryTransfer { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>KỶ LUẬT THÉP: Chuyển Lô nào từ Kho A thì Kho B nhận đúng Lô đó</summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Số lượng điều chuyển theo ĐVT</summary>
        public decimal Quantity { get; set; }
    }
}
