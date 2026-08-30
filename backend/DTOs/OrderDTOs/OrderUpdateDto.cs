using backend.Models.Enums;

namespace backend.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO Yêu cầu Cập nhật Đơn Bán Hàng (Order Partial Update / Patch).
    /// Thiết kế theo cơ chế Partial Update (Tất cả các trường đều Nullable): 
    /// Frontend chỉ cần truyền lên những trường cần thay đổi, các trường để null sẽ được Service bỏ qua (không ghi đè).
    /// </summary>
    public class OrderUpdateDto
    {
        #region Vận hành & Trạng thái (Operations & Status)
        /// <summary>
        /// Trạng thái vòng đời của đơn hàng.
        /// NGHIỆP VỤ LÕI: Việc thay đổi trạng thái ở đây có thể kích hoạt các Event/Trigger ngầm trong Service.
        /// Ví dụ: Chuyển sang "Cancelled" -> Kích hoạt hoàn lại Tồn kho đã Reserve. 
        /// Chuyển sang "Completed" -> Kích hoạt trừ Tồn kho thực tế.
        /// </summary>
        public OrderStatus? Status { get; set; }

        /// <summary>
        /// Trạng thái thanh toán của dòng tiền.
        /// Thường được cập nhật tự động qua Webhook của Cổng thanh toán (VNPay/MoMo), 
        /// hoặc được Kế toán/Thu ngân cập nhật thủ công khi nhận tiền mặt (COD).
        /// </summary>
        public PaymentStatus? PaymentStatus { get; set; }

        /// <summary>
        /// Kho xuất hàng.
        /// NGHIỆP VỤ ĐIỀU PHỐI (Manual Routing): Dùng khi Điều phối viên muốn can thiệp thủ công, 
        /// chuyển đổi kho xuất hàng khác so với kết quả của thuật toán Smart Routing ban đầu 
        /// (Ví dụ: Kho A bị sự cố mất điện, cần đẩy đơn sang Kho B xử lý).
        /// </summary>
        public int? WarehouseId { get; set; }
        #endregion

        #region Ghi chú & Xử lý ngoại lệ (Notes & Exceptions)
        /// <summary>
        /// Ghi chú nội bộ của Admin, CSKH hoặc Điều phối viên (Ví dụ: "Đã gọi khách hẹn giao lại vào ngày mai").
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Lý do hủy đơn hàng.
        /// KỶ LUẬT VẬN HÀNH: Bắt buộc hoặc rất được khuyến khích truyền lên nếu trường [Status] 
        /// được cập nhật thành Cancelled. Dữ liệu này dùng để lên báo cáo nguyên nhân rớt đơn (Drop-off Analysis).
        /// </summary>
        public string? CancellationReason { get; set; }
        #endregion
    }
}