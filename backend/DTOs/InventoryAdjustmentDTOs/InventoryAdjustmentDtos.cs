using backend.Models.Enums;

namespace backend.DTOs.InventoryAdjustmentDTOs
{
    public class InventoryAdjustmentDetailCreateDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
        public InventoryAdjustmentType AdjustmentType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ReasonDetail { get; set; }
    }

    public class InventoryAdjustmentCreateDto
    {
        public int WarehouseId { get; set; }
        public int? AuditId { get; set; }
        public InventoryAdjustmentReason Reason { get; set; } = InventoryAdjustmentReason.Surplus;
        public int? CreatedById { get; set; }
        public string? Note { get; set; }

        public List<InventoryAdjustmentDetailCreateDto> Details { get; set; } = new();
    }

    public class InventoryAdjustmentDetailReadDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int BatchId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public InventoryAdjustmentType AdjustmentType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }

        public string? ReasonDetail { get; set; }
    }

    public class InventoryAdjustmentReadDto
    {
        public int Id { get; set; }
        public string AdjustmentCode { get; set; } = string.Empty;

        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int? AuditId { get; set; }
        public string? AuditCode { get; set; }

        public InventoryAdjustmentStatus Status { get; set; }
        public InventoryAdjustmentReason Reason { get; set; }

        public int CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }

        public DateTime AdjustmentDate { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public decimal TotalVarianceAmount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InventoryAdjustmentDetailReadDto> Details { get; set; } = new();
    }
}
