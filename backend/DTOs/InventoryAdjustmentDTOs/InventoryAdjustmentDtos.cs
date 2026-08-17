using System.ComponentModel.DataAnnotations;
using backend.Models.Enums;

namespace backend.DTOs.InventoryAdjustmentDTOs
{
    public class InventoryAdjustmentDetailCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm biến thể")]
        public int VariantId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lô hàng")]
        public int BatchId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính")]
        public int UoMId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại điều chỉnh")]
        public InventoryAdjustmentType AdjustmentType { get; set; }

        [Range(0.0001, 1000000, ErrorMessage = "Số lượng điều chỉnh phải lớn hơn 0")]
        public decimal Quantity { get; set; }

        [Range(0, 100000000000, ErrorMessage = "Đơn giá không được âm")]
        public decimal UnitPrice { get; set; }

        [StringLength(500, ErrorMessage = "Chi tiết lý do tối đa 500 ký tự")]
        public string? ReasonDetail { get; set; }
    }

    public class InventoryAdjustmentCreateDto
    {
        [Required(ErrorMessage = "Vui lòng chọn kho hàng xảy ra biến động")]
        public int WarehouseId { get; set; }

        public int? AuditId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lý do điều chỉnh")]
        public InventoryAdjustmentReason Reason { get; set; } = InventoryAdjustmentReason.Surplus;

        public int? CreatedById { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Danh sách chi tiết không được để trống")]
        [MinLength(1, ErrorMessage = "Phiếu điều chỉnh phải có ít nhất 1 dòng chi tiết")]
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
