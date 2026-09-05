using backend.DTOs;
using backend.DTOs.CustomerReturnDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service cốt lõi quản lý Phiếu Khách Hàng Trả Hàng (Customer Return / RMA).
    /// Chịu trách nhiệm toàn bộ vòng đời thu hồi hàng hóa: Từ lúc tiếp nhận yêu cầu, 
    /// qua khâu Kiểm định chất lượng (QC) tại kho, cho đến khi chốt số liệu Hoàn tiền (Refund) 
    /// và Điều hướng tồn kho (Available vs Damaged).
    /// </summary>
    public interface ICustomerReturnService
    {
        #region Truy vấn & Phân quyền Dữ liệu (Read & Data Isolation)
        /// <summary>
        /// Lấy danh sách Phiếu trả hàng có phân trang, hỗ trợ lọc đa chiều cho Kế toán và Thủ kho.
        /// NGHIỆP VỤ DATA ISOLATION (Cách ly dữ liệu): Tham số [allowedWarehouseIds] đảm bảo Thủ kho 
        /// chỉ nhìn thấy các phiếu trả hàng được chỉ định gửi về kho của mình, tránh lộ lọt dữ liệu chéo chi nhánh.
        /// </summary>
        Task<PagedResult<CustomerReturnReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Truy xuất chi tiết một Phiếu trả hàng để Thủ kho tiến hành kiểm định hoặc Kế toán đối soát.
        /// Có cơ chế bảo mật xác thực quyền truy cập kho (allowedWarehouseIds).
        /// </summary>
        Task<CustomerReturnReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        #endregion

        #region Khởi tạo Yêu cầu (RMA Initiation)
        /// <summary>
        /// BƯỚC 1: Khởi tạo Yêu cầu trả hàng (RMA Request).
        /// NGHIỆP VỤ LÕI: Phiếu sinh ra ở trạng thái 'Pending'. Ở bước này, hệ thống TUYỆT ĐỐI CHƯA 
        /// cộng lại số dư Tồn kho vì hàng vật lý chưa về tới kho và chưa trải qua bước kiểm định (QC).
        /// </summary>
        Task<int> CreateAsync(CustomerReturnCreateDto dto, int? currentUserId = null);
        #endregion

        #region Duyệt & Kiểm định & Hạch toán (Lifecycle Workflow)
        /// <summary>
        /// Duyệt Yêu Cầu Trả Hàng (Pending -> Approved).
        /// Khách hàng sẽ thấy phiếu chuyển sang trạng thái "Đã duyệt" và chuẩn bị gửi hàng về kho.
        /// </summary>
        Task<bool> ApproveAsync(int id, int approvedById);

        /// <summary>
        /// BƯỚC 2: Kiểm định chất lượng tại kho (Approved -> Inspecting).
        /// Phân loại số lượng Đạt (Accepted) và Hỏng (Damaged) để chuẩn bị tạo phiếu nhập kho thu hồi.
        /// </summary>
        Task<bool> InspectQCAsync(int id, int receivedById, CustomerReturnInspectionDto dto);

        /// <summary>
        /// Hoàn tất trực tiếp Phiếu trả hàng (Inspecting -> Completed).
        /// Tự động cập nhật số dư tồn kho (Available / Damaged) và ghi nhận sổ cái bất biến.
        /// </summary>
        Task<bool> CompleteReturnAsync(int id);

        /// <summary>
        /// Kiểm định chất lượng và Hoàn tất phiếu trả hàng trong 1 bước (backward compatibility).
        /// </summary>
        Task<bool> InspectAndCompleteAsync(int id, int receivedById, CustomerReturnInspectionDto dto);

        /// <summary>
        /// Từ chối Yêu cầu trả hàng (Ví dụ: Khách gửi hàng không đúng, hoặc đã quá hạn đổi trả).
        /// Chuyển trạng thái phiếu sang 'Rejected', đóng luồng xử lý mà không làm thay đổi số dư kho.
        /// Bắt buộc phải có lý do từ chối để CSKH phản hồi lại cho khách.
        /// </summary>
        Task<bool> RejectReturnAsync(int id, string reason);
        #endregion

        #region Quản trị Hệ thống (Admin Options)
        /// <summary>
        /// Xóa mềm phiếu trả hàng (Soft Delete).
        /// Nghiệp vụ an toàn: Chỉ nên cho phép xóa khi phiếu đang ở trạng thái Pending. 
        /// Nếu phiếu đã Completed (tức là đã hạch toán Tồn kho và Dòng tiền), hệ thống nên chặn thao tác này.
        /// </summary>
        Task<bool> DeleteAsync(int id);
        #endregion
    }
}