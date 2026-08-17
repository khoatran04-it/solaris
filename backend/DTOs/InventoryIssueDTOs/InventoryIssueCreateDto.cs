namespace backend.DTOs.InventoryIssueDTOs
{
    public class InventoryIssueCreateDto
    {
        public int? OrderId { get; set; }
        public int WarehouseId { get; set; }
        public int? IssuedById { get; set; }
        public DateTime? IssueDate { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Note { get; set; }

        public List<InventoryIssueDetailCreateDto> Details { get; set; } = new();
    }

    public class InventoryIssueDetailCreateDto
    {
        public int? OrderDetailId { get; set; }
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
