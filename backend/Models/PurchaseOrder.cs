using backend.Models.Enums;
using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Đơn Đặt Mua Hàng (Purchase Order - PO).
    /// Quản lý thỏa thuận thu mua hàng hóa (nông sản, nguyên vật liệu) từ Nhà cung cấp (Supplier).
    /// Theo dõi tiến độ giao nhận, làm căn cứ để lập Phiếu Nhập Kho (Goods Receipt Note) và đối chiếu Công nợ phải trả.
    /// </summary>
    public class PurchaseOrder : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Trạng thái
        /// <summary>Mã chứng từ duy nhất trên hệ thống (Ví dụ: PO-20260817-001). Dùng để tra cứu, in ấn và liên kết với mã vận đơn của nhà cung cấp.</summary>
        public required string OrderCode { get; set; }

        /// <summary>
        /// Trạng thái vòng đời của đơn mua hàng (Nháp, Đang xử lý, Đã duyệt, Hoàn tất, Đã hủy).
        /// Nghiệp vụ: Đơn chỉ được chuyển sang 'Hoàn tất' (Completed) khi Thủ kho đã xác nhận nhập đủ hàng và Kế toán đã ghi nhận công nợ.
        /// </summary>
        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
        #endregion

        #region Thời gian & Kế hoạch
        /// <summary>Ngày chính thức lập đơn và chốt thỏa thuận đặt hàng với đối tác.</summary>
        public DateTime OrderDate { get; set; }

        /// <summary>Ngày hẹn giao hàng dự kiến từ nhà cung cấp (Deadline để Kho hàng chuẩn bị vị trí lưu trữ và nhân sự bốc dỡ).</summary>
        public DateTime? ExpectedDeliveryDate { get; set; }
        #endregion

        #region Chi phí & Nội dung
        /// <summary>Tổng giá trị cuối cùng của đơn đặt hàng (VND) (Bao gồm tiền hàng, sau khi trừ chiết khấu và cộng thuế).</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Giá trị quyết toán thực tế chốt theo số lượng thực nhận (VND).
        /// Null nếu đơn chưa hoàn tất hoặc nhận đủ 100% theo TotalAmount ban đầu.
        /// </summary>
        public decimal? SettledAmount { get; set; }

        /// <summary>Ghi chú hoặc yêu cầu đặc biệt gửi đến Nhà cung cấp (Ví dụ: Yêu cầu xe bảo ôn chuyên dụng, giao trước 8h sáng...).</summary>
        public string? Note { get; set; }

        /// <summary>Lý do hủy đơn (Nghiệp vụ: Bắt buộc yêu cầu nhân viên nhập lý do nếu trạng thái Status chuyển sang Cancelled).</summary>
        public string? CancellationReason { get; set; }

        /// <summary>Lý do chốt đóng đơn sớm theo thực nhận (khi NCC không giao bù hàng bị từ chối).</summary>
        public string? ClosureReason { get; set; }
        #endregion

        #region Hệ thống & Soft Delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết Đối tượng (Navigation Properties)
        /// <summary>Mã định danh của Nhà cung cấp (Supplier) tiếp nhận và thực hiện đơn hàng này.</summary>
        public int SupplierId { get; set; }

        /// <summary>Thực thể Đối tác / Nhà cung cấp.</summary>
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Mã định danh của Nhân viên Thu mua (Purchasing Staff / Buyer) chịu trách nhiệm lập và theo dõi tiến độ đơn hàng.</summary>
        public int CreatedById { get; set; }

        /// <summary>Thực thể Nhân viên lập chứng từ.</summary>
        public virtual IAUser? CreatedBy { get; set; }

        /// <summary>Mã định danh của Kho nhận hàng (Destination Warehouse). Theo quy định chuỗi cung ứng SCM, đơn mua từ NCC chỉ được giao về Kho Tổng.</summary>
        public int? WarehouseId { get; set; }

        /// <summary>Thực thể Kho nhận hàng.</summary>
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Danh sách chi tiết các mặt hàng (SKU), số lượng và đơn giá thỏa thuận trong hợp đồng đặt mua này.</summary>
        public virtual ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();
        #endregion
    }
}