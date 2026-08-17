namespace backend.Models
{
    public class CustomerReturnDetail
    {
        public int Id { get; set; }

        public int CustomerReturnId { get; set; }
        public virtual CustomerReturn? CustomerReturn { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // Lô hàng được khách trả lại
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        public decimal ReturnedQuantity { get; set; }

        // Kết quả kiểm định QC:
        // Hàng còn nguyên vẹn -> Nhập lại QuantityAvailable
        public decimal AcceptedQuantity { get; set; } = 0;

        // Hàng móp méo / hỏng hóc -> Nhập vào QuantityDamaged
        public decimal DamagedQuantity { get; set; } = 0;

        public decimal UnitPrice { get; set; } = 0;
        public decimal RefundAmount { get; set; } = 0;

        public string? RejectReason { get; set; }
    }
}
