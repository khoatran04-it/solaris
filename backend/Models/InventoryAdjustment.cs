using backend.Models.Enums;
using backend.Models.Interfaces;

namespace backend.Models
{
    public class InventoryAdjustment : ISoftDelete, IAuditable
    {
        public int Id { get; set; }
        public string AdjustmentCode { get; set; } = string.Empty;

        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public int? AuditId { get; set; }
        public InventoryAudit? Audit { get; set; }

        public InventoryAdjustmentStatus Status { get; set; } = InventoryAdjustmentStatus.Draft;
        public InventoryAdjustmentReason Reason { get; set; } = InventoryAdjustmentReason.Surplus;

        public int CreatedById { get; set; }
        public IAUser? CreatedBy { get; set; }

        public int? ApprovedById { get; set; }
        public IAUser? ApprovedBy { get; set; }

        public DateTime AdjustmentDate { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public decimal TotalVarianceAmount { get; set; }
        public string? Note { get; set; }

        // ISoftDelete & IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryAdjustmentDetail> Details { get; set; } = new List<InventoryAdjustmentDetail>();
    }

    public class InventoryAdjustmentDetail
    {
        public int Id { get; set; }

        public int AdjustmentId { get; set; }
        public InventoryAdjustment? Adjustment { get; set; }

        public int VariantId { get; set; }
        public ProductVariant? Variant { get; set; }

        public int BatchId { get; set; }
        public ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public UoM? UoM { get; set; }

        public InventoryAdjustmentType AdjustmentType { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }

        public string? ReasonDetail { get; set; }
    }
}
