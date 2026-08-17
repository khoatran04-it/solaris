using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Đợt Kiểm Kê Kho Hàng (Stocktake / Audit).
    /// Hỗ trợ kiểm kê toàn bộ, cuốn chiếu hoặc đột xuất.
    /// Tự động chụp ảnh số dư hệ thống (Snapshot) và hỗ trợ chế độ Đếm Mù (Blind Count).
    /// </summary>
    public class InventoryAudit : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã đợt kiểm kê (Ví dụ: AUDIT-20260817-001)</summary>
        public string AuditCode { get; set; } = string.Empty;

        /// <summary>Kho hàng được kiểm kê</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Hình thức: Toàn bộ (Full), Cuốn chiếu (Cycle), Đột xuất (Spot)</summary>
        public InventoryAuditType AuditType { get; set; } = InventoryAuditType.Full;

        /// <summary>Trạng thái: Nháp, Đang kiểm đếm, Chờ duyệt, Đã chốt sổ, Đã hủy</summary>
        public InventoryAuditStatus Status { get; set; } = InventoryAuditStatus.Draft;

        /// <summary>Nhân viên phụ trách kiểm đếm</summary>
        public int AuditorId { get; set; }
        public virtual IAUser? Auditor { get; set; }

        /// <summary>Người phê duyệt chốt số liệu</summary>
        public int? ApprovedById { get; set; }
        public virtual IAUser? ApprovedBy { get; set; }

        /// <summary>Ngày bắt đầu kiểm kê</summary>
        public DateTime AuditDate { get; set; }

        /// <summary>Ngày hoàn tất và chốt sổ kiểm kê</summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>Tổng số lượng tồn trên sổ sách hệ thống tại thời điểm tạo phiếu</summary>
        public decimal TotalSystemQty { get; set; }

        /// <summary>Tổng số lượng đếm được ngoài thực tế</summary>
        public decimal TotalActualQty { get; set; }

        /// <summary>Tổng số lượng chênh lệch = TotalActualQty - TotalSystemQty</summary>
        public decimal TotalVarianceQty { get; set; }

        /// <summary>Tổng giá trị chênh lệch (VND)</summary>
        public decimal TotalVarianceAmount { get; set; }

        /// <summary>Ghi chú & Chỉ đạo kiểm kê</summary>
        public string? Note { get; set; }

        // --- ISoftDelete & AUDIT FIELDS ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Danh sách chi tiết các mặt hàng kiểm kê</summary>
        public virtual ICollection<InventoryAuditDetail> Details { get; set; } = new List<InventoryAuditDetail>();
    }

    /// <summary>
    /// Dòng chi tiết mặt hàng trong Phiếu kiểm kê kho.
    /// </summary>
    public class InventoryAuditDetail
    {
        public int Id { get; set; }

        public int AuditId { get; set; }
        public virtual InventoryAudit? Audit { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Số lượng tồn hệ thống ghi nhận lúc mở đợt kiểm kê (Snapshot)</summary>
        public decimal SystemQuantity { get; set; }

        /// <summary>Số lượng thực tế nhân viên đếm được</summary>
        public decimal ActualQuantity { get; set; }

        /// <summary>Chênh lệch = ActualQuantity - SystemQuantity</summary>
        public decimal VarianceQuantity { get; set; }

        /// <summary>Đơn giá vốn tính toán giá trị chênh lệch</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Giá trị chênh lệch = VarianceQuantity * UnitPrice</summary>
        public decimal VarianceAmount { get; set; }

        /// <summary>Giải trình / ghi chú của nhân viên kiểm đếm</summary>
        public string? ReasonNote { get; set; }
    }
}
