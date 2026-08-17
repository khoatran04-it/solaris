namespace backend.DTOs.CustomerReturnDTOs
{
    public class CustomerReturnCreateDto
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public int? ReceivedById { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? Reason { get; set; }

        public List<CustomerReturnDetailCreateDto> Details { get; set; } = new();
    }

    public class CustomerReturnDetailCreateDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
        public decimal ReturnedQuantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class CustomerReturnInspectionDto
    {
        public string? InspectionNotes { get; set; }
        public List<CustomerReturnItemInspectionDto> Items { get; set; } = new();
    }

    public class CustomerReturnItemInspectionDto
    {
        public int DetailId { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal DamagedQuantity { get; set; }
        public string? RejectReason { get; set; }
    }
}
