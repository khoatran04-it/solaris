using backend.Models.Enums;

namespace backend.DTOs.InventoryTransferDTOs
{
    public class InventoryTransferReadDto
    {
        public int Id { get; set; }
        public string TransferCode { get; set; } = string.Empty;

        public int FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;

        public int ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;

        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }

        public InventoryTransferStatus Status { get; set; }

        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public int? DispatchedById { get; set; }
        public string? DispatchedByName { get; set; }
        public DateTime? DispatchedDate { get; set; }

        public int? ReceivedById { get; set; }
        public string? ReceivedByName { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InventoryTransferDetailReadDto> Details { get; set; } = new();
    }

    public class InventoryTransferDetailReadDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
    }
}
