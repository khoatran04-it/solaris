using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Nhập Kho & Kiểm Đếm Chất Lượng (Goods/Inventory Receipt Note - GRN).
    /// Ghi nhận quy trình vật lý khi xe giao hàng cập bến tại cửa kho. 
    /// Đóng vai trò là chốt chặn QC (Quality Control) để phân loại hàng Đạt (Accepted) / Hỏng (Rejected) 
    /// và là chứng từ duy nhất được phép làm TĂNG số dư Tồn kho trên hệ thống.
    /// </summary>
    public class InventoryReceipt : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã phiếu nhập kho (Ví dụ: IR-20260817-001). Dùng để đối chiếu chứng từ giấy và dán lên pallet chờ kiểm.</summary>
        public required string ReceiptCode { get; set; }

        /// <summary>
        /// Trạng thái vòng đời của Phiếu nhập (Pending: Chờ kiểm, QC: Đang kiểm tra, Completed: Hoàn tất, Cancelled: Đã hủy).
        /// Nghiệp vụ cốt lõi: Số dư Tồn kho (Inventory) CHỈ ĐƯỢC CỘNG TĂNG LÊN khi phiếu này chuyển sang trạng thái Completed.
        /// </summary>
        public InventoryReceiptStatus Status { get; set; } = InventoryReceiptStatus.Pending;
        #endregion

        #region Thời gian & Ghi chú
        /// <summary>Thời điểm xe giao hàng cập bến và bắt đầu bốc dỡ thực tế tại cửa kho.</summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>Ghi chú tổng quan về đợt nhận hàng (Ví dụ: Xe giao trễ 2 tiếng, bao bì ướt nhẹ do mưa...).</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy phiếu (Bắt buộc nhập nếu Status chuyển sang Cancelled).</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Hệ thống & Soft Delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết Đối tượng (Navigation Properties)
        /// <summary>Mã định danh của Kho hàng trực tiếp tiếp nhận và lưu trữ lô hàng này.</summary>
        public int WarehouseId { get; set; }

        /// <summary>Thực thể Kho hàng.</summary>
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>
        /// Nhà cung cấp giao hàng. 
        /// (Có thể null trong trường hợp nhập kho nội bộ hoặc nhập hàng hoàn trả, không qua đường Thu mua).
        /// </summary>
        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Mã định danh Thủ kho hoặc Nhân viên QC trực tiếp ký nhận và chịu trách nhiệm kiểm đếm.</summary>
        public int? ReceivedById { get; set; }
        public virtual IAUser? ReceivedBy { get; set; }

        /// <summary>Danh sách chi tiết các mặt hàng thực tế được bốc xuống và kiểm đếm.</summary>
        public virtual ICollection<InventoryReceiptDetail> Details { get; set; } = new List<InventoryReceiptDetail>();
        #endregion
    }
}