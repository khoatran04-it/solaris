using backend.DTOs;
using backend.DTOs.InventoryAdjustmentDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Quản lý Phiếu Điều Chỉnh, Xuất Hủy & Cân Bằng Tồn Kho (Inventory Adjustment).
    /// Đóng vai trò là "Van an toàn" của hệ thống, cho phép hợp thức hóa các khoản chênh lệch, 
    /// hao hụt tự nhiên, hư hỏng, hoặc cân bằng lại sổ sách sau đợt kiểm kê.
    /// </summary>
    public interface IInventoryAdjustmentService
    {
        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <summary>
        /// Lấy danh sách Phiếu điều chỉnh kèm phân trang và bộ lọc chuyên sâu.
        /// Thường dùng cho Kế toán kho rà soát các khoản thất thoát định kỳ.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã phiếu).</param>
        /// <param name="warehouseId">Lọc theo kho hàng xảy ra biến động.</param>
        /// <param name="status">Lọc theo trạng thái quy trình (Nháp, Đã duyệt, Đã hủy).</param>
        /// <param name="reason">Lọc theo nguyên nhân (Hao hụt, Hư hỏng, Mất cắp, Dư kiểm kê...).</param>
        /// <param name="allowedWarehouseIds">[Bảo mật RBAC] Danh sách ID kho mà User có quyền truy cập.</param>
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
        /// Lấy chi tiết phiếu điều chỉnh theo ID kèm toàn bộ các dòng biến động.
        /// (Có kiểm tra bảo mật phân quyền theo kho).
        /// </summary>
        Task<InventoryAdjustmentReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        #endregion

        #region Khởi tạo & Quy trình Xử lý (Command & Workflow)
        /// <summary>
        /// BƯỚC 1: Lập Phiếu đề xuất Điều chỉnh mới.
        /// Phiếu khởi tạo sẽ mặc định ở trạng thái Nháp (Draft). 
        /// Nghiệp vụ an toàn: Việc tạo phiếu lúc này TUYỆT ĐỐI CHƯA làm thay đổi số dư Tồn kho thực tế.
        /// </summary>
        Task<int> CreateAsync(InventoryAdjustmentCreateDto dto);

        /// <summary>
        /// BƯỚC 2: Phê duyệt Phiếu điều chỉnh (Nghiệp vụ Hạch toán Lõi).
        /// Khi Quản lý/Kế toán trưởng bấm duyệt, Service mở Unit of Work thực hiện:
        /// 1. Cập nhật Status thành "Approved" và lưu ApprovedDate.
        /// 2. Duyệt qua từng dòng Detail, đọc Enum [AdjustmentType] để gọi hàm tương ứng trong Core IInventoryService:
        ///    - IncreaseAvailable: Cộng vào Hàng Xanh (Available).
        ///    - DecreaseAvailable: Trừ thẳng Hàng Xanh.
        ///    - MoveToDamaged: Trừ Hàng Xanh, Cộng vào Hàng Đỏ (Damaged).
        ///    - WriteOffDamaged: Trừ thẳng Hàng Đỏ (Đem đi tiêu hủy).
        /// 3. IInventoryService sẽ tự động ghi Log các bút toán này vào Sổ cái (InventoryTransaction).
        /// </summary>
        /// <param name="id">ID Phiếu điều chỉnh cần duyệt.</param>
        /// <param name="approvedById">ID Người có thẩm quyền phê duyệt.</param>
        Task<bool> ApproveAdjustmentAsync(int id, int approvedById);
        #endregion

        #region Hủy bỏ & Dọn dẹp (Cancellation & Soft Delete)
        /// <summary>
        /// Hủy phiếu đề xuất điều chỉnh.
        /// Ràng buộc: Chỉ được phép hủy khi phiếu đang ở trạng thái Nháp (Draft) hoặc Đang chờ duyệt.
        /// Nếu phiếu đã Approved (đã tác động tới Sổ cái), hệ thống sẽ ném Exception chặn lại.
        /// </summary>
        Task<bool> CancelAsync(int id, string reason);

        /// <summary>
        /// Xóa mềm phiếu điều chỉnh khỏi hệ thống (Soft Delete).
        /// Áp dụng tương tự quy tắc Hủy: Không được phép xóa chứng từ đã làm thay đổi số dư kế toán.
        /// </summary>
        Task<bool> DeleteAsync(int id);
        #endregion
    }
}