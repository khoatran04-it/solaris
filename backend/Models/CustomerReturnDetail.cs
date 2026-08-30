using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Chi tiết Phiếu Trả Hàng (Customer Return Detail).
    /// Ghi nhận chi tiết số lượng hàng hóa khách trả lại và kết quả phân loại chất lượng (QC).
    /// Đóng vai trò là đầu vào trực tiếp cho Core Engine tồn kho để phân bổ lại hàng hóa.
    /// </summary>
    public class CustomerReturnDetail
    {
        public int Id { get; set; }

        #region Liên kết Phiếu gốc
        public int CustomerReturnId { get; set; }
        public virtual CustomerReturn? CustomerReturn { get; set; }
        #endregion

        #region Hàng hóa & Nguồn gốc (Traceability)
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// Lô hàng nông sản bị trả lại.
        /// KỶ LUẬT TRUY VẾT: Bắt buộc phải xác định được lô hàng gốc để tính lại hạn sử dụng 
        /// và đánh giá xem Nhà cung cấp lô hàng này có thường xuyên bị khách phàn nàn hay không.
        /// </summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Phân loại Chất lượng (QC & Inventory Routing)
        /// <summary>Tổng số lượng khách mang trả cho mặt hàng này.</summary>
        public decimal ReturnedQuantity { get; set; }

        /// <summary>
        /// KẾT QUẢ QC 1: Số lượng còn nguyên vẹn, đạt chuẩn.
        /// NGHIỆP VỤ KHO: Số lượng này sẽ được IInventoryService tự động cộng ngược 
        /// trở lại Hàng Khả dụng (QuantityAvailable) để tiếp tục bán cho khách khác.
        /// </summary>
        public decimal AcceptedQuantity { get; set; } = 0;

        /// <summary>
        /// KẾT QUẢ QC 2: Số lượng bị dập nát, móp méo, hư hỏng.
        /// NGHIỆP VỤ KHO: Số lượng này tuyệt đối KHÔNG được cộng vào Hàng Khả dụng, 
        /// mà sẽ bị đẩy vào Hàng Hỏng (QuantityDamaged) chờ cấp quản lý tạo Phiếu Điều Chỉnh Xuất Hủy (Write-off).
        /// </summary>
        public decimal DamagedQuantity { get; set; } = 0;
        #endregion

        #region Tài chính & Hoàn tiền (Financials & Refund)
        /// <summary>Đơn giá dùng làm cơ sở tính toán số tiền hoàn trả (Thường lấy từ OrderDetail gốc).</summary>
        public decimal UnitPrice { get; set; } = 0;

        /// <summary>
        /// Số tiền thực tế hoàn trả cho khách trên dòng sản phẩm này.
        /// Tùy chính sách công ty, có thể hoàn đủ 100% (ReturnedQuantity * UnitPrice) 
        /// hoặc chỉ hoàn tiền cho phần hàng đạt chuẩn (AcceptedQuantity * UnitPrice) nếu lỗi hỏng do khách gây ra.
        /// </summary>
        public decimal RefundAmount { get; set; } = 0;
        #endregion

        #region Ghi chú & Giải trình
        /// <summary>
        /// Lý do từ chối hoàn tiền hoặc ghi chú chi tiết về tình trạng lỗi.
        /// (Ví dụ: "Cà chua bị dập nát do khách bóc hộp sai cách, từ chối hoàn tiền phần hàng hỏng").
        /// </summary>
        public string? RejectReason { get; set; }
        #endregion
    }
}