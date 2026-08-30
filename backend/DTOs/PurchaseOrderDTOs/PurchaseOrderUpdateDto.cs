using backend.Models.Enums;
using System;

namespace backend.DTOs.PurchaseOrderDTOs
{
    /// <summary>
    /// DTO yêu cầu Cập nhật thông tin Đơn Đặt Mua Hàng (Purchase Order).
    /// Nghiệp vụ: Để bảo vệ tính toàn vẹn của chứng từ tài chính, hệ thống thường chỉ cho phép cập nhật 
    /// lịch giao hàng, bổ sung ghi chú, hoặc thay đổi trạng thái (Duyệt, Hủy).
    /// Việc thay đổi số lượng, giá tiền hay nhà cung cấp sẽ bị khóa, hoặc phải qua một API/luồng xử lý đặc thù khác.
    /// </summary>
    public class PurchaseOrderUpdateDto
    {
        #region Lịch trình & Ghi chú
        /// <summary>Ngày hẹn giao hàng dự kiến (Cho phép cập nhật dời ngày nếu Nhà cung cấp báo giao trễ).</summary>
        public DateTime? ExpectedDeliveryDate { get; set; }

        /// <summary>Ghi chú hoặc yêu cầu vận hành (Cho phép bổ sung thêm thông tin trong quá trình chờ hàng).</summary>
        public string? Note { get; set; }
        #endregion

        #region Trạng thái Chứng từ (Workflow)
        /// <summary>
        /// Trạng thái vòng đời của đơn mua hàng.
        /// Dùng để chạy luồng công việc (Workflow): Chuyển từ Draft (Nháp) -> Processing (Đang xử lý) -> Completed (Hoàn tất) hoặc Cancelled (Đã hủy).
        /// </summary>
        public PurchaseOrderStatus Status { get; set; }

        /// <summary>
        /// Lý do hủy đơn.
        /// Nghiệp vụ: FluentValidation sẽ bắt buộc trường này không được để trống nếu Status được cập nhật thành Cancelled.
        /// </summary>
        public string? CancellationReason { get; set; }
        #endregion
    }
}