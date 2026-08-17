using backend.DTOs;
using backend.DTOs.InventoryAuditDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Quản lý Quy trình Kiểm kê Kho Hàng (Inventory Audit / Stocktake).
    /// </summary>
    public interface IInventoryAuditService
    {
        /// <summary>
        /// Lấy danh sách đợt kiểm kê phân trang, hỗ trợ bộ lọc và Data-Level Authorization.
        /// </summary>
        Task<PagedResult<InventoryAuditReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            int? auditType,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Lấy chi tiết đợt kiểm kê theo ID kèm toàn bộ dòng kiểm đếm.
        /// </summary>
        Task<InventoryAuditReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);

        /// <summary>
        /// Khởi tạo đợt kiểm kê mới: Tự động chụp Snapshot số dư hệ thống tại thời điểm tạo.
        /// </summary>
        Task<int> CreateAsync(InventoryAuditCreateDto dto);

        /// <summary>
        /// Cập nhật số lượng kiểm đếm thực tế (Actual Quantity) và ghi nhận chênh lệch.
        /// </summary>
        Task<bool> SubmitCountAsync(int id, InventoryAuditSubmitCountDto dto);

        /// <summary>
        /// Phê duyệt chốt kiểm kê: Tự động tạo Phiếu Điều Chỉnh (InventoryAdjustment) để cân bằng sổ cái.
        /// </summary>
        Task<int> ApproveAndReconcileAsync(int id, int approvedById);

        /// <summary>
        /// Hủy đợt kiểm kê.
        /// </summary>
        Task<bool> CancelAsync(int id, string reason);
    }
}
