using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Điều Chỉnh & Xuất Hủy Tồn Kho (Inventory Adjustment / Write-off).
    /// Đóng vai trò là chứng từ "Bù trừ và Hợp thức hóa".
    /// Xử lý các tình huống thực tế: Hao hụt tự nhiên, hư hỏng, dập nát, quá hạn sử dụng, mất cắp, 
    /// hoặc tự động sinh ra để cân bằng số liệu sau một đợt Kiểm kê (Audit).
    /// </summary>
    public class InventoryAdjustment : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã chứng từ điều chỉnh (Ví dụ: ADJ-20260817-001).</summary>
        public string AdjustmentCode { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái quy trình (Draft: Nháp, Approved: Đã duyệt/Đã ghi sổ, Cancelled: Đã hủy).
        /// Nghiệp vụ cốt lõi: Chỉ khi trạng thái là Approved, Core Engine (IInventoryService) 
        /// mới được phép can thiệp để Tăng/Giảm số dư trên hệ thống và ghi Log Sổ cái.
        /// </summary>
        public InventoryAdjustmentStatus Status { get; set; } = InventoryAdjustmentStatus.Draft;

        /// <summary>
        /// Lý do tổng quan của đợt điều chỉnh (Surplus: Thừa kiểm kê, Shrinkage: Hao hụt tự nhiên, 
        /// Spoilage: Hư hỏng/Thối rữa, Expired: Hết HSD, Theft: Mất cắp).
        /// Phục vụ cho Báo cáo phân tích nguyên nhân thất thoát.
        /// </summary>
        public InventoryAdjustmentReason Reason { get; set; } = InventoryAdjustmentReason.Surplus;
        #endregion

        #region Địa điểm & Đối soát
        /// <summary>Kho hàng nơi xảy ra sự cố chênh lệch/hao hụt.</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>
        /// ID Đợt kiểm kê gốc. 
        /// Nghiệp vụ: Nếu phiếu điều chỉnh này được sinh ra TỰ ĐỘNG từ màn hình Chốt sổ Kiểm kê (InventoryAudit), 
        /// trường này bắt buộc phải có giá trị để Kế toán truy vết nguồn gốc bút toán.
        /// </summary>
        public int? AuditId { get; set; }
        public virtual InventoryAudit? Audit { get; set; }
        #endregion

        #region Nhân sự & Thời gian
        /// <summary>Nhân viên kho phát hiện sự cố và lập phiếu đề xuất điều chỉnh.</summary>
        public int CreatedById { get; set; }
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Ngày lập chứng từ đề xuất.</summary>
        public DateTime AdjustmentDate { get; set; }

        /// <summary>Quản lý kho / Kế toán trưởng phê duyệt và chịu trách nhiệm về khoản thất thoát này.</summary>
        public int? ApprovedById { get; set; }
        public virtual IAUser? ApprovedBy { get; set; }

        /// <summary>Ngày chính thức phê duyệt và hệ thống thực thi cập nhật sổ cái.</summary>
        public DateTime? ApprovedDate { get; set; }
        #endregion

        #region Tài chính & Ghi chú
        /// <summary>
        /// Tổng giá trị tiền tệ của đợt điều chỉnh (VND).
        /// Rất quan trọng để Kế toán đưa vào hạch toán chi phí (Giá vốn hàng bán / Chi phí bất thường).
        /// </summary>
        public decimal TotalVarianceAmount { get; set; }

        /// <summary>Biên bản giải trình chi tiết đính kèm (Ví dụ: "Kho bị dột do mưa bão đêm 16/08 làm ướt 5 thùng cà chua").</summary>
        public string? Note { get; set; }
        #endregion

        #region Hệ thống & Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết
        /// <summary>Danh sách chi tiết các mặt hàng cần điều chỉnh tăng/giảm hoặc xuất hủy.</summary>
        public virtual ICollection<InventoryAdjustmentDetail> Details { get; set; } = new List<InventoryAdjustmentDetail>();
        #endregion
    }

    /// <summary>
    /// Thực thể Chi tiết Phiếu Điều Chỉnh (Inventory Adjustment Detail).
    /// </summary>
    public class InventoryAdjustmentDetail
    {
        public int Id { get; set; }

        #region Hàng hóa & Nguồn gốc (Traceability)
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// KỶ LUẬT THÉP: Ngay cả khi vứt bỏ hàng hỏng, hệ thống vẫn bắt buộc phải ghi nhận chính xác 
        /// đó là hàng của Lô (Batch) nào để trừ đúng tồn kho và đánh giá chất lượng của Nhà cung cấp lô đó.
        /// </summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Loại Điều chỉnh & Khối lượng
        /// <summary>
        /// NGHIỆP VỤ LÕI: Phân loại hành động điều chỉnh.
        /// - IncreaseAvailable: Cộng thêm vào Hàng khả dụng (Do dư kiểm kê).
        /// - DecreaseAvailable: Trừ thẳng Hàng khả dụng (Do hao hụt, mất cắp).
        /// - MoveToDamaged: Chuyển từ Xanh (Available) sang Đỏ (Damaged) - Hàng dập nát chờ quản lý ra quyết định.
        /// - WriteOffDamaged: Xuất hủy vĩnh viễn Hàng Đỏ (Damaged) ra khỏi kho đem đi tiêu hủy.
        /// </summary>
        public InventoryAdjustmentType AdjustmentType { get; set; } = InventoryAdjustmentType.DecreaseAvailable;

        /// <summary>
        /// Số lượng biến động. 
        /// Nguyên tắc kế toán: Luôn là SỐ DƯƠNG tuyệt đối (Absolute value). 
        /// Việc nó là Tăng hay Giảm sẽ do trường [AdjustmentType] quyết định.
        /// </summary>
        public decimal Quantity { get; set; }
        #endregion

        #region Tài chính & Giải trình
        /// <summary>Đơn giá vốn của mặt hàng tại thời điểm điều chỉnh.</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Thành tiền chênh lệch (Quantity * UnitPrice) phục vụ báo cáo Kế toán.</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Diễn giải chi tiết lý do cho từng mặt hàng (Ví dụ: "Dập nát đáy thùng").</summary>
        public string? ReasonDetail { get; set; }
        #endregion

        #region Đối soát Chứng từ
        public int AdjustmentId { get; set; }
        public virtual InventoryAdjustment? Adjustment { get; set; }
        #endregion
    }
}