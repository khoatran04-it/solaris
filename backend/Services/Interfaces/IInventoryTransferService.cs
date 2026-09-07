using backend.DTOs;
using backend.DTOs.InventoryTransferDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Phiếu Điều Chuyển Liên Kho (Inventory Transfer).
    /// Chịu trách nhiệm điều phối luồng luân chuyển hàng hóa vật lý, đảm bảo nguyên tắc 2 bước chặt chẽ:
    /// Xuất đi (Dispatched) -> Nhập nhận đích (Received) để không làm thất thoát số dư trên sổ cái.
    /// </summary>
    public interface IInventoryTransferService
    {
        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <summary>
        /// Lấy danh sách Phiếu điều chuyển kèm phân trang và bộ lọc chuyên sâu.
        /// </summary>
        /// <param name="allowedWarehouseIds">
        /// [Bảo mật RBAC] Danh sách ID kho mà User có quyền truy cập. 
        /// Nghiệp vụ: User sẽ được xem phiếu này nếu Kho nguồn (From) HOẶC Kho đích (To) nằm trong danh sách quyền.
        /// </param>
        Task<PagedResult<InventoryTransferReadDto>> GetPagedAsync(
            string? search,
            int? fromWarehouseId,
            int? toWarehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Lấy chi tiết Phiếu điều chuyển theo ID. (Có kiểm tra bảo mật phân quyền kho).
        /// </summary>
        Task<InventoryTransferReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        #endregion

        #region Khởi tạo & Hủy lệnh (Command)
        /// <summary>
        /// Lập phiếu điều chuyển mới (Trạng thái mặc định: Draft - Nháp).
        /// Tùy theo chính sách kho, bước này có thể kích hoạt cơ chế Giữ chỗ (Reserve) tồn kho tại Kho Nguồn.
        /// </summary>
        Task<int> CreateAsync(InventoryTransferCreateDto dto);

        /// <summary>
        /// Hủy lệnh điều chuyển.
        /// Ràng buộc: Chỉ được phép hủy khi hàng chưa được xuất đi (Status = Draft). 
        /// Nếu hàng đã lên xe (Dispatched), bắt buộc phải làm quy trình nhập lại kho nguồn (Hoàn trả) thay vì bấm xóa/hủy.
        /// </summary>
        Task<bool> CancelTransferAsync(int id, string reason);
        #endregion

        #region Quy trình 2 bước: Điều chuyển vật lý (2-Step Workflow)
        /// <summary>
        /// BƯỚC 1: Xuất hàng đi (Dispatch).
        /// Mở Transaction (Unit of Work) để thực hiện:
        /// 1. Gọi IInventoryService.DeductStockAsync() để TRỪ tồn kho tại Kho Nguồn (FromWarehouse).
        /// 2. Ghi nhận thời điểm xuất hàng thực tế (DispatchedDate).
        /// 3. Chuyển trạng thái phiếu sang Dispatched (Hàng đang đi đường / In Transit).
        /// </summary>
        /// <param name="id">ID Phiếu điều chuyển.</param>
        /// <param name="dispatchedById">ID Thủ kho Nguồn thực hiện xuất hàng.</param>
        Task<bool> DispatchTransferAsync(int id, int dispatchedById);

        /// <summary>
        /// BƯỚC 2: Nhận hàng tại đích (Receive / Chốt sổ).
        /// Mở Transaction (Unit of Work) để thực hiện:
        /// 1. Gọi IInventoryService.AddStockAsync() để CỘNG tồn kho tại Kho Đích (ToWarehouse).
        ///    (Bắt buộc phải giữ nguyên Mã Lô - BatchId để bảo toàn Truy xuất nguồn gốc).
        /// 2. Ghi nhận thời điểm nhận hàng thực tế (ReceivedDate).
        /// 3. Chuyển trạng thái phiếu sang Received (Hoàn tất).
        /// </summary>
        /// <param name="id">ID Phiếu điều chuyển.</param>
        /// <param name="receivedById">ID Thủ kho Đích thực hiện nhận hàng.</param>
        Task<bool> ReceiveTransferAsync(int id, int receivedById);

        /// <summary>
        /// BƯỚC 2 (NÂNG CAO): Kiểm đếm và Nhập nhận đích (Inspect & Receive).
        /// Cho phép phân luồng số lượng nguyên vẹn vào QuantityAvailable và số lượng hư hỏng vào QuantityDamaged.
        /// </summary>
        Task<bool> InspectAndReceiveTransferAsync(int id, int inspectedById, InventoryTransferInspectReceiveDto dto);
        #endregion
    }
}