using backend.Models.Enums;
using backend.Models.Interfaces;

namespace backend.Models
{
    public class InventoryAudit : ISoftDelete, IAuditable
    {
        public int Id { get; set; }
        public string AuditCode { get; set; } = string.Empty;

        public int WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public InventoryAuditType AuditType { get; set; } = InventoryAuditType.Full;
        public InventoryAuditStatus Status { get; set; } = InventoryAuditStatus.Draft;

        public int AuditorId { get; set; }
        public IAUser? Auditor { get; set; }

        public int? ApprovedById { get; set; }
        public IAUser? ApprovedBy { get; set; }

        public DateTime AuditDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public decimal TotalSystemQty { get; set; }
        public decimal TotalActualQty { get; set; }
        public decimal TotalVarianceQty { get; set; }
        public decimal TotalVarianceAmount { get; set; }

        public string? Note { get; set; }

        // ISoftDelete & IAuditable
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InventoryAuditDetail> Details { get; set; } = new List<InventoryAuditDetail>();
    }

    public class InventoryAuditDetail
    {
        public int Id { get; set; }

        public int AuditId { get; set; }
        public InventoryAudit? Audit { get; set; }

        public int VariantId { get; set; }
        public ProductVariant? Variant { get; set; }

        public int BatchId { get; set; }
        public ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public UoM? UoM { get; set; }

        public decimal SystemQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal VarianceQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal VarianceAmount { get; set; }

        public string? ReasonNote { get; set; }
    }
}
