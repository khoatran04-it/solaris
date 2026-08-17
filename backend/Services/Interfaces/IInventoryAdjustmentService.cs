using backend.DTOs;
using backend.DTOs.InventoryAdjustmentDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Quản lý Phiếu Điều Chỉnh, Xuất Hủy & Cân Bằng Tồn Kho (Inventory Adjustment).
    /// </summary>
    public interface IInventoryAdjustmentService
    {
        /// <summary>
        /// Lấy danh sách phiếu điều chỉnh phân trang, hỗ trợ lọc theo kho, trạng thái, lý do và phân quyền.
        /// </summary>
        Task<PagedResult<InventoryAdjustmentReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            int? reason,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Lấy chi tiết phiếu điều chỉnh theo ID kèm các dòng biến động.
        /// </summary>
        Task<InventoryAdjustmentReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);

        /// <summary>
        /// Tạo mới phiếu điều chỉnh ở trạng thái Nháp (Draft).
        /// </summary>
        Task<int> CreateAsync(InventoryAdjustmentCreateDto dto);

        /// <summary>
        /// Phê duyệt phiếu điều chỉnh: Cập nhật trực tiếp số dư két sắt 4 ngăn và ghi log giao dịch vào sổ cái.
        /// </summary>
        Task<bool> ApproveAdjustmentAsync(int id, int approvedById);

        /// <summary>
        /// Hủy phiếu điều chỉnh.
        /// </summary>
        Task<bool> CancelAsync(int id, string reason);

        /// <summary>
        /// Xóa mềm phiếu điều chỉnh (chỉ áp dụng cho phiếu Nháp).
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
