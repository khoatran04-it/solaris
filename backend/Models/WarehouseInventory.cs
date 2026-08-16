namespace backend.Models
{
    public class WarehouseInventory
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // 🔥 KỶ LUẬT THÉP: Không có hàng hóa nào được nằm trong kho mà không có Lô (Bỏ dấu ?)
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        // 4 TRẠNG THÁI TỒN KHO
        public decimal QuantityAvailable { get; set; } = 0;
        public decimal QuantityReserved { get; set; } = 0;
        public decimal QuantityQC { get; set; } = 0;
        public decimal QuantityDamaged { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}