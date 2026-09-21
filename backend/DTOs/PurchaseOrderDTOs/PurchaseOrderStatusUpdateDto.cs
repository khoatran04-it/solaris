using backend.Models.Enums;
using System;

namespace backend.DTOs.PurchaseOrderDTOs
{
    /// <summary>
    /// DTO yêu cầu Cập nhật trạng thái vòng đời hoặc thông tin vận hành của Đơn Đặt Mua Hàng (Purchase Order).
    /// Hỗ trợ luồng công việc (Workflow): Chuyển trạng thái từ Nháp (Draft) -> Đang xử lý (Processing) -> Đã duyệt (Approved) -> Hoàn tất (Completed) hoặc Đã hủy (Cancelled).
    /// </summary>
    public class PurchaseOrderStatusUpdateDto
    {
        #region Trạng thái Chứng từ (Workflow)
        /// <summary>
        /// Trạng thái vòng đời mới của đơn mua hàng.
        /// </summary>
        public PurchaseOrderStatus Status { get; set; }

        /// <summary>
        /// Lý do hủy đơn.
        /// Nghiệp vụ: Bắt buộc không được để trống khi Status chuyển sang trạng thái Cancelled (Đã hủy).
        /// </summary>
        public string? CancellationReason { get; set; }
        #endregion

        #region Lịch trình & Ghi chú Vận hành
        /// <summary>
        /// Ngày hẹn giao hàng dự kiến được điều chỉnh từ Nhà cung cấp (nếu có dời lịch giao).
        /// </summary>
        public DateTime? ExpectedDeliveryDate { get; set; }

        /// <summary>
        /// Ghi chú bổ sung trong quá trình theo dõi và thực hiện đơn hàng.
        /// </summary>
        public string? Note { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO yêu cầu Chốt đóng đơn mua hàng sớm theo số lượng thực nhận (Settle & Close PO).
    /// </summary>
    public class ClosePurchaseOrderDto
    {
        /// <summary>Lý do chốt đóng đơn (bắt buộc, ví dụ: NCC không giao bù hàng bị từ chối/hỏng).</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
