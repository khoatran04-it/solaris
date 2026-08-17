using backend.Models.Enums;

namespace backend.DTOs.InventoryAuditDTOs
{
    public class InventoryAuditDetailCreateDto
    {
        public int VariantId { get; set; }
        public int BatchId { get; set; }
        public int UoMId { get; set; }
    }

    public class InventoryAuditCreateDto
    {
        public int WarehouseId { get; set; }
        public InventoryAuditType AuditType { get; set; } = InventoryAuditType.Full;
        public int? AuditorId { get; set; }
        public string? Note { get; set; }

        // Tùy chọn kiểm kê cuốn chiếu theo danh mục hoặc danh sách sản phẩm
        public List<InventoryAuditDetailCreateDto>? SpecificItems { get; set; }
    }

    public class InventoryAuditItemCountDto
    {
        public int DetailId { get; set; }
        public decimal ActualQuantity { get; set; }
        public string? ReasonNote { get; set; }
    }

    public class InventoryAuditSubmitCountDto
    {
        public List<InventoryAuditItemCountDto> Items { get; set; } = new();
        public string? Note { get; set; }
    }

    public class InventoryAuditDetailReadDto
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

        public decimal SystemQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal VarianceQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal VarianceAmount { get; set; }

        public string? ReasonNote { get; set; }
    }

    public class InventoryAuditReadDto
    {
        public int Id { get; set; }
        public string AuditCode { get; set; } = string.Empty;

        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public InventoryAuditType AuditType { get; set; }
        public InventoryAuditStatus Status { get; set; }

        public int AuditorId { get; set; }
        public string AuditorName { get; set; } = string.Empty;

        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }

        public DateTime AuditDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public decimal TotalSystemQty { get; set; }
        public decimal TotalActualQty { get; set; }
        public decimal TotalVarianceQty { get; set; }
        public decimal TotalVarianceAmount { get; set; }

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<InventoryAuditDetailReadDto> Details { get; set; } = new();
    }
}
