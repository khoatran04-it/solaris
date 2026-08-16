namespace backend.DTOs.PurchaseOrderDTOs
{
    public class PurchaseOrderCreateDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Note { get; set; }

        public int SupplierId { get; set; }
        public int CreatedById { get; set; }

        public List<PurchaseOrderDetailCreateDto> Details { get; set; } = new();
    }

    public class PurchaseOrderDetailCreateDto
    {
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public decimal OrderQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
