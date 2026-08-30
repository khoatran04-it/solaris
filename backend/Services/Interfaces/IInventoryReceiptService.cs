using backend.DTOs;
using backend.DTOs.InventoryReceiptDTOs;
using System;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Phiếu Nhập Kho (Inventory Receipt / GRN).
    /// Chịu trách nhiệm xử lý toàn bộ vòng đời của chứng từ nhập kho: từ khâu khởi tạo chờ kiểm đếm, 
    /// ghi nhận kết quả QC, cho đến khi chốt sổ và chính thức cộng số dư Tồn kho.
    /// </summary>
    public interface IInventoryReceiptService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy danh sách Phiếu nhập kho kèm phân trang và bộ lọc đa chiều.
        /// Thường dùng để render DataGrid trên màn hình Quản lý Kho của Admin/Thủ kho.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Ví dụ: Mã phiếu nhập).</param>
        /// <param name="warehouseId">Lọc phiếu theo Kho tiếp nhận.</param>
        /// <param name="supplierId">Lọc phiếu theo Nhà cung cấp giao hàng.</param>
        /// <param name="status">Lọc theo trạng thái (Pending, QC, Completed, Cancelled).</param>
        /// <param name="startDate">Lọc từ ngày (Dựa trên ReceiptDate).</param>
        /// <param name="endDate">Lọc đến ngày (Dựa trên ReceiptDate).</param>
        /// <param name="pageIndex">Trang hiện tại (Bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<InventoryReceiptReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? supplierId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy chi tiết Phiếu nhập kho theo ID.
        /// Dữ liệu trả về bao gồm danh sách kết quả kiểm đếm (Accepted/Rejected) và các trường đã được làm phẳng.
        /// </summary>
        Task<InventoryReceiptReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu & Quy trình (Command & Workflow)
        /// <summary>
        /// Khởi tạo Phiếu nhập kho mới với kết quả kiểm đếm ban đầu.
        /// Nghiệp vụ an toàn: Dữ liệu lúc này chỉ lưu vào vỏ chứng từ, TUYỆT ĐỐI CHƯA CỘNG VÀO TỒN KHO.
        /// Trạng thái mặc định thường là Pending.
        /// </summary>
        Task<int> CreateAsync(InventoryReceiptCreateDto dto);

        /// <summary>
        /// NGHIỆP VỤ LÕI: Hoàn tất Phiếu nhập kho (Chốt sổ).
        /// Khi hàm này được gọi, Service bắt buộc phải mở 1 Transaction (Unit of Work) để thực hiện đồng thời 3 việc:
        /// 1. Cập nhật Status của Phiếu nhập thành Completed.
        /// 2. Gọi IInventoryService.AddStockAsync() để TĂNG số dư Tồn kho thực tế dựa trên số lượng [AcceptedQuantity].
        /// 3. Cập nhật ngược lại số lượng đã nhận [ReceivedQuantity] vào các dòng PurchaseOrder tương ứng (Nếu nhập từ PO).
        /// </summary>
        /// <param name="id">Mã định danh Phiếu nhập.</param>
        /// <param name="receivedById">Mã nhân viên / Thủ kho thực hiện chốt sổ.</param>
        /// <param name="note">Ghi chú bổ sung lúc hoàn tất (Nếu có).</param>
        Task<bool> CompleteReceiptAsync(int id, int receivedById, string? note);

        /// <summary>
        /// Hủy Phiếu nhập kho.
        /// Bắt buộc phải cung cấp lý do. Chỉ cho phép hủy khi phiếu chưa ở trạng thái Completed.
        /// </summary>
        Task<bool> CancelReceiptAsync(int id, string reason);

        /// <summary>
        /// Xóa Phiếu nhập kho (Soft Delete).
        /// Ràng buộc: Chỉ được xóa những phiếu đang ở trạng thái Pending. 
        /// Nếu phiếu đã Completed (đã làm thay đổi số dư kho), Service sẽ ném ra Exception chặn thao tác xóa ngay lập tức.
        /// </summary>
        Task<bool> DeleteAsync(int id);
        #endregion
    }
}