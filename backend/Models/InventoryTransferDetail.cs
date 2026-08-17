namespace backend.Models
{
    public class InventoryTransferDetail
    {
        public int Id { get; set; }

        public int InventoryTransferId { get; set; }
        public virtual InventoryTransfer? InventoryTransfer { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // KỶ LUẬT THÉP: Chuyển Lô nào từ Kho A thì Kho B nhận đúng Lô đó
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        public decimal Quantity { get; set; }
    }
}
