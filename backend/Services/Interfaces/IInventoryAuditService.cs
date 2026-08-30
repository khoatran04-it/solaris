using backend.DTOs;
using backend.DTOs.InventoryAuditDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Quản lý Quy trình Kiểm kê Kho Hàng (Inventory Audit / Stocktake).
    /// Đóng vai trò là "Trọng tài", giúp đối soát giữa số liệu trên hệ thống và thực tế tại kho.
    /// </summary>
    public interface IInventoryAuditService
    {
        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <summary>
        /// Lấy danh sách đợt kiểm kê phân trang, hỗ trợ bộ lọc và Data-Level Authorization.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã đợt kiểm kê, Ghi chú).</param>
        /// <param name="warehouseId">Lọc theo kho cụ thể.</param>
        /// <param name="status">Lọc theo trạng thái kiểm kê.</param>
        /// <param name="auditType">Lọc theo loại hình (Full, Cycle, Spot).</param>
        /// <param name="allowedWarehouseIds">[Bảo mật RBAC] Danh sách ID kho mà User có quyền truy cập.</param>
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
        #endregion

        #region Quy trình 1: Khởi tạo & Đếm thực tế (Snapshot & Blind Count)
        /// <summary>
        /// BƯỚC 1: Khởi tạo đợt kiểm kê mới.
        /// NGHIỆP VỤ LÕI: Hệ thống sẽ tự động "Chụp ảnh số dư" (Snapshot) toàn bộ hoặc một phần 
        /// số lượng tồn kho (QuantityAvailable) tại kho đó để lưu vào trường [SystemQuantity].
        /// Trạng thái phiếu sẽ chuyển sang "Counting" (Đang kiểm đếm).
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo đợt kiểm kê.</param>
        Task<int> CreateAsync(InventoryAuditCreateDto dto);

        /// <summary>
        /// BƯỚC 2: Nộp kết quả kiểm đếm thực tế (Blind Count Submit).
        /// Hỗ trợ nộp nhiều lần. Hệ thống tự động lấy [ActualQuantity] trừ đi [SystemQuantity] (Snapshot) 
        /// để tính ra [VarianceQuantity] (Chênh lệch) và [VarianceAmount] (Thành tiền chênh lệch).
        /// </summary>
        /// <param name="id">ID đợt kiểm kê.</param>
        /// <param name="dto">Danh sách số lượng thực tế nhân viên đếm được.</param>
        Task<bool> SubmitCountAsync(int id, InventoryAuditSubmitCountDto dto);
        #endregion

        #region Quy trình 2: Chốt sổ & Bù trừ (Reconciliation)
        /// <summary>
        /// BƯỚC 3: Phê duyệt chốt kiểm kê (Approve & Reconcile).
        /// Nghiệp vụ khép kín (Unit of Work): 
        /// 1. Cập nhật Status của đợt kiểm kê thành "Completed" (Đã hoàn tất).
        /// 2. QUAN TRỌNG: Quét các dòng có VarianceQuantity != 0, tự động gom nhóm và sinh ra một 
        ///    Phiếu Điều Chỉnh (InventoryAdjustment) để Kế toán xử lý. 
        /// Service KHÔNG trực tiếp gọi IInventoryService.Add/Deduct ở đây để đảm bảo Sổ cái luôn 
        /// có chứng từ Điều chỉnh (Adjustment) giải trình.
        /// </summary>
        /// <param name="id">ID đợt kiểm kê.</param>
        /// <param name="approvedById">ID Quản lý / Kế toán thực hiện chốt sổ.</param>
        /// <returns>Trả về ID của Phiếu Điều Chỉnh (InventoryAdjustment) vừa được sinh tự động.</returns>
        Task<int> ApproveAndReconcileAsync(int id, int approvedById);

        /// <summary>
        /// Hủy đợt kiểm kê.
        /// Chỉ được phép hủy khi trạng thái chưa chuyển sang Completed.
        /// </summary>
        Task<bool> CancelAsync(int id, string reason);
        #endregion
    }
}