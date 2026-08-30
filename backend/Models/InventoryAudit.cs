using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Đợt Kiểm Kê Kho Hàng (Inventory Audit / Stocktake).
    /// Đóng vai trò là chứng từ "Đối soát Thực tế và Hệ thống". 
    /// Nghiệp vụ cốt lõi: Hỗ trợ "Chụp ảnh số dư" (Snapshot) lúc bắt đầu kiểm kê để đối chiếu, 
    /// và tự động sinh ra các bút toán điều chỉnh (Adjustment) nếu có chênh lệch khi chốt sổ.
    /// </summary>
    public class InventoryAudit : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Phân loại
        /// <summary>Mã đợt kiểm kê duy nhất (Ví dụ: AUDIT-20260817-001).</summary>
        public string AuditCode { get; set; } = string.Empty;

        /// <summary>
        /// Hình thức kiểm kê (Full: Toàn bộ kho, Cycle: Cuốn chiếu/Theo khu vực, Spot: Đột xuất một vài mặt hàng).
        /// </summary>
        public InventoryAuditType AuditType { get; set; } = InventoryAuditType.Full;

        /// <summary>
        /// Trạng thái quy trình (Draft, Counting, PendingApproval, Completed, Cancelled).
        /// Nghiệp vụ: Chỉ khi trạng thái chuyển sang Completed, hệ thống mới ghi đè số dư thực tế vào Sổ cái.
        /// </summary>
        public InventoryAuditStatus Status { get; set; } = InventoryAuditStatus.Draft;
        #endregion

        #region Địa điểm & Nhân sự
        /// <summary>Kho hàng diễn ra đợt kiểm kê.</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Nhân viên thực hiện đi đếm hàng thực tế (Auditor).</summary>
        public int AuditorId { get; set; }
        public virtual IAUser? Auditor { get; set; }

        /// <summary>Quản lý kho / Kế toán trưởng phê duyệt kết quả kiểm kê để chốt sổ.</summary>
        public int? ApprovedById { get; set; }
        public virtual IAUser? ApprovedBy { get; set; }
        #endregion

        #region Thời gian & Ghi chú
        /// <summary>Ngày bắt đầu đợt kiểm kê (Thời điểm bấm nút Snapshot tồn kho hệ thống).</summary>
        public DateTime AuditDate { get; set; }

        /// <summary>Thời điểm hoàn tất và chính thức chốt sổ liệu điều chỉnh.</summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>Ghi chú hoặc chỉ đạo từ cấp trên (Ví dụ: "Tập trung kiểm tra khu vực hàng cận Date").</summary>
        public string? Note { get; set; }
        #endregion

        #region Tổng hợp Chênh lệch (Reconciliation Summary)
        /// <summary>Tổng số lượng hàng hóa ghi nhận trên sổ sách (Hệ thống) tại thời điểm Snapshot.</summary>
        public decimal TotalSystemQty { get; set; }

        /// <summary>Tổng số lượng hàng hóa nhân viên đếm được ngoài thực tế.</summary>
        public decimal TotalActualQty { get; set; }

        /// <summary>
        /// Tổng số lượng chênh lệch (TotalActualQty - TotalSystemQty).
        /// Có thể là số Âm (Thiếu hàng / Thất thoát) hoặc số Dương (Dư hàng).
        /// </summary>
        public decimal TotalVarianceQty { get; set; }

        /// <summary>Tổng giá trị tiền tệ của phần chênh lệch (VND) để Kế toán hạch toán vào chi phí/thu nhập.</summary>
        public decimal TotalVarianceAmount { get; set; }
        #endregion

        #region Hệ thống & Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết
        /// <summary>Danh sách chi tiết các mặt hàng và lô hàng được kiểm đếm trong đợt này.</summary>
        public virtual ICollection<InventoryAuditDetail> Details { get; set; } = new List<InventoryAuditDetail>();
        #endregion
    }

    /// <summary>
    /// Thực thể Chi tiết Phiếu Kiểm Kê (Inventory Audit Detail).
    /// </summary>
    public class InventoryAuditDetail
    {
        public int Id { get; set; }

        #region Hàng hóa & Lô hàng (Traceability)
        /// <summary>Biến thể sản phẩm (SKU) đang được kiểm đếm.</summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP: Việc kiểm kê bắt buộc phải chi tiết đến từng Lô hàng (Batch).
        /// Nếu kho phát hiện một thùng hàng không dán mã lô, nhân viên phải khai báo nó thành một dòng riêng để chờ xử lý.
        /// </summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        /// <summary>Đơn vị tính dùng để kiểm đếm (Nên quy về Base UoM để tính toán chính xác nhất).</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Số liệu Snapshot & Đếm thực tế (Blind Count)
        /// <summary>
        /// Tồn kho hệ thống lúc tạo phiếu (Snapshot).
        /// Nghiệp vụ "Đếm mù" (Blind Count): Để chống gian lận, Frontend/App của nhân viên đi đếm 
        /// sẽ BỊ ẨN con số này. Họ buộc phải đếm thực tế rồi nhập số vào.
        /// </summary>
        public decimal SystemQuantity { get; set; }

        /// <summary>Số lượng thực tế nhân viên đếm được tại vị trí kho.</summary>
        public decimal ActualQuantity { get; set; }
        #endregion

        #region Phân tích Chênh lệch (Variance Analysis)
        /// <summary>Chênh lệch = ActualQuantity - SystemQuantity (Âm = Thiếu, Dương = Thừa).</summary>
        public decimal VarianceQuantity { get; set; }

        /// <summary>Đơn giá vốn của lô hàng này (Dùng để tính ra số tiền thất thoát/dư thừa).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Giá trị chênh lệch = VarianceQuantity * UnitPrice.</summary>
        public decimal VarianceAmount { get; set; }

        /// <summary>Giải trình của nhân viên nếu phát hiện chênh lệch (Ví dụ: "Hàng bị chuột cắn góc kho, chưa báo hủy").</summary>
        public string? ReasonNote { get; set; }
        #endregion

        #region Đối soát Chứng từ
        public int AuditId { get; set; }
        public virtual InventoryAudit? Audit { get; set; }
        #endregion
    }
}