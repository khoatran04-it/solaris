using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiếu Xuất Kho (Inventory Issue / Goods Issue Note).
    /// Chứng từ ghi nhận quá trình nhặt hàng (Pick) và đóng gói (Pack) để giao cho khách hoặc luân chuyển nội bộ.
    /// Nghiệp vụ cốt lõi: Khi phiếu này Hoàn tất, hệ thống sẽ thực hiện trừ Tồn kho thực tế, 
    /// đồng thời giải phóng số dư Giữ chỗ (Reserved) đã được khóa lại từ lúc khách đặt Đơn hàng.
    /// </summary>
    public class InventoryIssue : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã phiếu xuất kho (Ví dụ: OUT-20260817-001). Dùng để in mã vạch cho nhân viên kho quét khi đi nhặt hàng.</summary>
        public required string IssueCode { get; set; }

        /// <summary>Thời điểm xuất kho thực tế (Bàn giao cho đơn vị vận chuyển / Shipper).</summary>
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Trạng thái của phiếu xuất (Pending: Chờ lấy hàng, Processing: Đang đóng gói, Completed: Đã xuất, Cancelled: Đã hủy).
        /// Nghiệp vụ: Các biến động trừ số dư Tồn kho (Inventory) chỉ được kích hoạt khi Status = Completed.
        /// </summary>
        public InventoryIssueStatus Status { get; set; } = InventoryIssueStatus.Pending;
        #endregion

        #region Đối soát Chứng từ
        /// <summary>
        /// Mã định danh Đơn bán hàng (Sales Order) tham chiếu yêu cầu xuất kho này.
        /// (Có thể null trong các nghiệp vụ: Xuất tiêu hủy hàng hỏng, Xuất mẫu test nội bộ...).
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>Thực thể Đơn bán hàng.</summary>
        public virtual Order? Order { get; set; }
        #endregion

        #region Thông tin Giao nhận
        /// <summary>Tên người nhận (Khách hàng cuối, hoặc Tên tài xế/Đơn vị vận chuyển).</summary>
        public string? ReceiverName { get; set; }

        /// <summary>Số điện thoại liên hệ giao hàng.</summary>
        public string? ReceiverPhone { get; set; }

        /// <summary>Địa chỉ đích đến của chuyến hàng.</summary>
        public string? DeliveryAddress { get; set; }
        #endregion

        #region Ghi chú & Quản trị rủi ro
        /// <summary>Ghi chú đóng gói hoặc vận chuyển (Ví dụ: Hàng dễ dập nát, cần chèn túi khí, bọc màng co cẩn thận...).</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy phiếu xuất kho (Bắt buộc cung cấp nếu Status chuyển sang Cancelled để kiểm toán nội bộ).</summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Hệ thống & Soft Delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết Đối tượng (Navigation Properties)
        /// <summary>Mã định danh Kho hàng thực hiện xuất lô hàng này.</summary>
        public int WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Mã định danh của Thủ kho / Nhân viên (Picker) trực tiếp đi nhặt hàng và đóng gói.</summary>
        public int IssuedById { get; set; }
        public virtual IAUser? IssuedBy { get; set; }

        /// <summary>Danh sách chi tiết các mặt hàng và Lô hàng (Batch) cụ thể được xuất đi.</summary>
        public virtual ICollection<InventoryIssueDetail> Details { get; set; } = new List<InventoryIssueDetail>();
        #endregion
    }
}