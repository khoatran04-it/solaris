using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Điều Chỉnh & Xuất Hủy Tồn Kho.
    /// Xử lý hao hụt tự nhiên, hư hỏng, dập nát, hết hạn, mất cắp hoặc cân bằng số liệu sau kiểm kê.
    /// </summary>
    public class InventoryAdjustment : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã chứng từ điều chỉnh (Ví dụ: ADJ-20260817-001)</summary>
        public string AdjustmentCode { get; set; } = string.Empty;

        /// <summary>ID Kho xảy ra biến động</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>ID Đợt kiểm kê gốc (nếu sinh tự động từ kiểm kê)</summary>
        public int? AuditId { get; set; }
        public virtual InventoryAudit? Audit { get; set; }

        /// <summary>Trạng thái phiếu: Nháp, Đã duyệt, Đã hủy</summary>
        public InventoryAdjustmentStatus Status { get; set; } = InventoryAdjustmentStatus.Draft;

        /// <summary>Lý do điều chỉnh (Hao hụt, Mất mát, Hư hỏng, Quá hạn, Thừa kiểm kê...)</summary>
        public InventoryAdjustmentReason Reason { get; set; } = InventoryAdjustmentReason.Surplus;

        /// <summary>Người lập phiếu</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Người phê duyệt và thực hiện cập nhật sổ cái</summary>
        public int? ApprovedById { get; set; }
        public virtual IAUser? ApprovedBy { get; set; }

        /// <summary>Ngày lập chứng từ</summary>
        public DateTime AdjustmentDate { get; set; }

        /// <summary>Ngày phê duyệt và áp dụng vào tồn kho</summary>
        public DateTime? ApprovedDate { get; set; }

        /// <summary>Tổng giá trị chênh lệch (VND)</summary>
        public decimal TotalVarianceAmount { get; set; }

        /// <summary>Ghi chú & Biên bản giải trình</summary>
        public string? Note { get; set; }

        // --- ISoftDelete & AUDIT FIELDS ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Danh sách chi tiết các mặt hàng điều chỉnh</summary>
        public virtual ICollection<InventoryAdjustmentDetail> Details { get; set; } = new List<InventoryAdjustmentDetail>();
    }

    /// <summary>
    /// Dòng chi tiết mặt hàng trong Phiếu điều chỉnh tồn kho.
    /// </summary>
    public class InventoryAdjustmentDetail
    {
        public int Id { get; set; }

        public int AdjustmentId { get; set; }
        public virtual InventoryAdjustment? Adjustment { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Loại điều chỉnh (Tăng khả dụng, Giảm khả dụng, Chuyển sang hàng hỏng, Xuất hủy hàng hỏng)</summary>
        public InventoryAdjustmentType AdjustmentType { get; set; } = InventoryAdjustmentType.DecreaseAvailable;

        /// <summary>Số lượng biến động (luôn là số dương)</summary>
        public decimal Quantity { get; set; }

        /// <summary>Đơn giá vốn tại thời điểm điều chỉnh</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Thành tiền = Quantity * UnitPrice</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Diễn giải chi tiết lý do</summary>
        public string? ReasonDetail { get; set; }
    }
}
