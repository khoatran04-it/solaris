using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Điểm dừng / Đơn hàng trong Chuyến xe (Delivery Trip Order).
    /// Ánh xạ một đơn hàng cụ thể vào một chuyến xe giao hàng B2C nội bộ.
    /// </summary>
    public class DeliveryTripOrder
    {
        public int Id { get; set; }

        #region Liên kết Chuyến & Đơn
        /// <summary>Mã định danh chuyến xe chủ quản.</summary>
        public int TripId { get; set; }
        public virtual DeliveryTrip? Trip { get; set; }

        /// <summary>Mã định danh đơn bán hàng (Order) được giao trong chuyến này.</summary>
        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }
        #endregion

        #region Thứ tự & Tiến độ
        /// <summary>Thứ tự điểm dừng giao hàng dự kiến (1, 2, 3... theo tuyến tối ưu).</summary>
        public int DeliverySequence { get; set; } = 1;

        /// <summary>Trạng thái giao cho riêng đơn này: "Pending" (Chờ giao), "Delivered" (Đã giao thành công), "Failed" (Giao thất bại/Khách vắng mặt).</summary>
        public string Status { get; set; } = "Pending";

        /// <summary>Thời điểm khách hàng ký nhận đơn thành công.</summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>Lý do giao hàng thất bại nếu có (Ví dụ: Không liên lạc được khách, sai địa chỉ...).</summary>
        public string? FailureReason { get; set; }

        /// <summary>Ghi chú của tài xế khi giao hàng.</summary>
        public string? Note { get; set; }
        #endregion
    }
}
