using backend.Models.Enums;

namespace backend.DTOs.InventoryReceiptDTOs
{
    public class InventoryReceiptReadDto
    {
        public int Id { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public InventoryReceiptStatus Status { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public int? ReceivedById { get; set; }
        public string? ReceivedByName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InventoryReceiptDetailReadDto> Details { get; set; } = new();
    }

    public class InventoryReceiptDetailReadDto
    {
        public int Id { get; set; }
        
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        
        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public int? PurchaseOrderDetailId { get; set; }

        public decimal ExpectedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string? RejectReason { get; set; }
    }
}
