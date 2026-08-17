namespace backend.Models
{
    public class InventoryIssueDetail
    {
        public int Id { get; set; }

        public int InventoryIssueId { get; set; }
        public virtual InventoryIssue? InventoryIssue { get; set; }

        public int? OrderDetailId { get; set; }
        public virtual OrderDetail? OrderDetail { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // KỶ LUẬT THÉP: Mọi dòng xuất kho phải chỉ đích danh Lô hàng (Batch)
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
