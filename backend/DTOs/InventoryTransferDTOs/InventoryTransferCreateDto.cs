namespace backend.DTOs.InventoryTransferDTOs
{
    public class InventoryTransferCreateDto
    {
        public int FromWarehouseId { get; set; }
        public int ToWarehouseId { get; set; }
        public int? OrderId { get; set; }
        public int? CreatedById { get; set; }
        public string? Note { get; set; }

        public List<InventoryTransferDetailCreateDto> Details { get; set; } = new();
    }

    public class InventoryTransferDetailCreateDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
    }
}
