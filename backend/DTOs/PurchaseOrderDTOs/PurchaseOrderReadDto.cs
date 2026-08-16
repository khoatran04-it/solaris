using backend.Models.Enums;

namespace backend.DTOs.PurchaseOrderDTOs
{
    public class PurchaseOrderReadDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<PurchaseOrderDetailReadDto> Details { get; set; } = new();
    }

    public class PurchaseOrderDetailReadDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public decimal OrderQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal ReceivedQuantity { get; set; }
    }
}
