using backend.DTOs;
using backend.DTOs.OrderDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service cốt lõi xử lý Đơn Bán Hàng (Order Service).
    /// Trái tim của phân hệ Bán hàng B2C, chịu trách nhiệm quản lý vòng đời đơn hàng, 
    /// kích hoạt thuật toán Định tuyến kho (Smart Routing) và tương tác với Tồn kho (Reservation).
    /// </summary>
    public interface IOrderService
    {
        #region Truy vấn & Phân quyền Dữ liệu (Read & Data Isolation)
        /// <summary>
        /// Lấy danh sách Đơn hàng có phân trang, hỗ trợ lọc đa chiều từ Admin Dashboard.
        /// NGHIỆP VỤ DATA ISOLATION (Cách ly dữ liệu): Tham số [allowedWarehouseIds] bảo đảm Nhân viên kho/Cửa hàng trưởng 
        /// chỉ nhìn thấy các đơn hàng được điều phối về kho của họ. Admin tổng sẽ truyền null để xem toàn bộ dữ liệu hệ thống.
        /// </summary>
        Task<PagedResult<OrderReadDto>> GetPagedAsync(
            string? search,
            int? customerId,
            int? warehouseId,
            int? status,
            int? paymentStatus,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Lấy chi tiết một Đơn hàng theo ID.
        /// Tích hợp cơ chế bảo mật: Kiểm tra chéo xem User hiện tại có quyền xem đơn hàng của Kho xuất này hay không.
        /// </summary>
        Task<OrderReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        #endregion

        #region Thao tác Vận hành (Command & Workflow)
        /// <summary>
        /// Khởi tạo Đơn bán hàng mới (Dành cho Admin tạo đơn thủ công hoặc luồng Checkout tự động).
        /// LUỒNG NGHIỆP VỤ LÕI: 
        /// 1. Snapshot (Chụp nhanh) thông tin địa chỉ và giá bán gốc.
        /// 2. Chạy Smart Routing để tìm Kho xuất hàng gần nhất có đủ tồn (Nếu Frontend truyền WarehouseId = null).
        /// 3. Gọi IInventoryService để Reserve (Giữ chỗ) tồn kho khả dụng, ngăn chặn triệt để rủi ro bán vượt số dư.
        /// </summary>
        Task<int> CreateAsync(OrderCreateDto dto, int? currentUserId = null);

        /// <summary>
        /// Cập nhật trạng thái Đơn hàng và dòng tiền (Partial Update).
        /// KÍCH HOẠT EVENT TỒN KHO: Tùy thuộc vào Status mới, Service sẽ gọi các luồng xử lý ngầm.
        /// (Ví dụ: Chuyển sang 'Completed' -> Lập phiếu xuất kho thực tế, trừ tồn kho vĩnh viễn và tạo mã truy vết lô).
        /// </summary>
        Task<bool> UpdateStatusAsync(int id, OrderUpdateDto dto);
        #endregion

        #region Hủy & Xóa (Cancellation & Deletion)
        /// <summary>
        /// Hủy đơn hàng đang xử lý.
        /// NGHIỆP VỤ BẢO VỆ TỒN KHO: Bắt buộc gọi qua IInventoryService để nhả (Release) lượng Tồn kho 
        /// đã được Giữ chỗ (Reserve) trước đó, giúp lượng hàng này quay lại trạng thái Available để bán tiếp.
        /// Bắt buộc ghi nhận lý do hủy (reason) để phục vụ Data Analytics.
        /// </summary>
        Task<bool> CancelAsync(int id, string reason, int? currentUserId = null);

        /// <summary>
        /// Xóa mềm đơn hàng khỏi hệ thống (Soft Delete).
        /// Thường chỉ Admin cấp cao mới có quyền thực hiện nhằm dọn dẹp các đơn test hoặc dữ liệu rác.
        /// </summary>
        Task<bool> DeleteAsync(int id);
        #endregion
    }
}