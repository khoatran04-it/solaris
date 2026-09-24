using backend.DTOs;
using backend.DTOs.PurchaseOrderDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Quản lý Đơn Đặt Mua Hàng (Purchase Order - PO).
    /// Hạt nhân thỏa thuận thu mua nông sản từ Nhà cung cấp, liên kết tiến độ giao nhận và làm căn cứ lập Phiếu Nhập Kho.
    /// </summary>
    public interface IPurchaseOrderService
    {
        #region Read Operations
        /// <summary>Lấy danh sách tất cả đơn mua hàng (phục vụ chọn lựa nhanh khi lập Phiếu nhập kho).</summary>
        Task<IEnumerable<PurchaseOrderReadDto>> GetAllListAsync();

        /// <summary>Tìm kiếm, lọc nâng cao và phân trang danh sách đơn mua hàng.</summary>
        Task<PagedResult<PurchaseOrderReadDto>> GetPagedAsync(
            string? search,
            int? supplierId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy thông tin chi tiết đơn mua hàng kèm danh sách mặt hàng chi tiết (Line Items).</summary>
        Task<PurchaseOrderReadDto> GetByIdAsync(int id);
        #endregion

        #region Write & Workflow Operations
        /// <summary>Tạo mới đơn đặt mua hàng ở trạng thái Nháp (Draft).</summary>
        Task<int> CreateAsync(PurchaseOrderCreateDto dto, int currentUserId);

        /// <summary>Chỉnh sửa toàn bộ thông tin đơn hàng và danh sách sản phẩm (Chỉ áp dụng khi đơn ở trạng thái Nháp).</summary>
        Task<bool> UpdateAsync(int id, PurchaseOrderCreateDto dto);

        /// <summary>Cập nhật trạng thái vòng đời đơn hàng (Duyệt, Đang giao, Hoàn tất hoặc Hủy đơn kèm lý do).</summary>
        Task<bool> UpdateStatusAsync(int id, PurchaseOrderStatusUpdateDto dto);

        /// <summary>
        /// Chốt đóng đơn mua hàng sớm theo số lượng thực nhận (Settle & Close PO).
        /// Áp dụng cho các đơn hàng ở trạng thái PartiallyReceived khi NCC không giao tiếp phần hàng thiếu/hỏng.
        /// Chuyển trạng thái sang Completed và cập nhật SettledAmount để chốt công nợ chính xác.
        /// </summary>
        Task<bool> CloseAndSettleOrderAsync(int id, string reason, int currentUserId);

        /// <summary>
        /// Ghi nhận thanh toán / giải ngân công nợ cho Đơn mua hàng (PO).
        /// Cập nhật lũy kế PaidAmount, tự động chuyển trạng thái PaymentStatus (PartiallyPaid / Paid).
        /// </summary>
        Task<PurchaseOrderReadDto> RecordPaymentAsync(int id, RecordPurchaseOrderPaymentDto dto, int currentUserId);

        /// <summary>Xóa mềm đơn đặt mua hàng (Chỉ cho phép xóa khi đơn còn ở trạng thái Nháp).</summary>
        Task<bool> DeleteAsync(int id);
        #endregion
    }
}