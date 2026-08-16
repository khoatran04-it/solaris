namespace backend.DTOs.InventoryReceiptDTOs
{
    public class InventoryReceiptCreateDto
    {
        public int WarehouseId { get; set; }
        public int? SupplierId { get; set; }
        public int? ReceivedById { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string? Note { get; set; }

        public List<InventoryReceiptDetailCreateDto> Details { get; set; } = new();
    }

    public class InventoryReceiptDetailCreateDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
        public int? PurchaseOrderDetailId { get; set; }
        public decimal ExpectedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string? RejectReason { get; set; }
    }
}
