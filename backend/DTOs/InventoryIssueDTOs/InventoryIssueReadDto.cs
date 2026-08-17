using backend.Models.Enums;

namespace backend.DTOs.InventoryIssueDTOs
{
    public class InventoryIssueReadDto
    {
        public int Id { get; set; }
        public string IssueCode { get; set; } = string.Empty;

        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }

        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int IssuedById { get; set; }
        public string IssuedByName { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }
        public InventoryIssueStatus Status { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InventoryIssueDetailReadDto> Details { get; set; } = new();
    }

    public class InventoryIssueDetailReadDto
    {
        public int Id { get; set; }
        public int? OrderDetailId { get; set; }

        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
