using backend.Models.Enums;

namespace backend.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }
        public required string TransactionCode { get; set; }
        public TransactionType Type { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // 🔥 KỶ LUẬT THÉP: Mọi giao dịch Nhập/Xuất/Khách trả đều phải chỉ đích danh là Lô nào (Bỏ dấu ?)
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public decimal Quantity { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Note { get; set; }

        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}