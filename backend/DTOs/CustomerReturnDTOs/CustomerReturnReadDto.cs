using backend.Models.Enums;

namespace backend.DTOs.CustomerReturnDTOs
{
    public class CustomerReturnReadDto
    {
        public int Id { get; set; }
        public string ReturnCode { get; set; } = string.Empty;

        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int? ReceivedById { get; set; }
        public string? ReceivedByName { get; set; }

        public DateTime ReturnDate { get; set; }
        public CustomerReturnStatus Status { get; set; }

        public decimal RefundAmount { get; set; }
        public string? Reason { get; set; }
        public string? InspectionNotes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<CustomerReturnDetailReadDto> Details { get; set; } = new();
    }

    public class CustomerReturnDetailReadDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public decimal ReturnedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal DamagedQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public string? RejectReason { get; set; }
    }
}
