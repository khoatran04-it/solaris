using backend.DTOs;
using backend.DTOs.InventoryDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Core Service quản lý Tồn kho (Inventory Engine).
    /// Đây là "Trái tim" của phân hệ kho, nơi duy nhất được quyền can thiệp vào bảng Inventory 
    /// để tính toán lại số lượng Khả dụng (Available) và Giữ chỗ (Reserved).
    /// Nghiệp vụ cốt lõi: Mọi thay đổi qua các hàm Core Engine đều BẮT BUỘC phải sinh ra một 
    /// dòng nhật ký bất biến trong Sổ cái (InventoryTransaction).
    /// </summary>
    public interface IInventoryService
    {
        #region Truy vấn & Báo cáo Tồn kho (Query & Reporting)
        /// <summary>
        /// Lấy toàn bộ danh sách tồn kho hiện tại (Thường dùng cho các tác vụ Export Excel hoặc chạy Job đồng bộ).
        /// </summary>
        /// <param name="allowedWarehouseIds">[Bảo mật RBAC] Danh sách ID kho được phép truy cập.</param>
        Task<IEnumerable<InventoryReadDto>> GetAllListAsync(List<int>? allowedWarehouseIds = null);

        /// <summary>
        /// Lấy Báo cáo tồn kho thời gian thực kèm phân trang và bộ lọc chuyên sâu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã/Tên sản phẩm, Mã Lô).</param>
        /// <param name="warehouseId">Lọc theo một kho cụ thể.</param>
        /// <param name="isExpiringSoon">
        /// Cực kỳ quan trọng: Lọc các lô hàng chuẩn bị hết hạn. 
        /// Phục vụ cho Dashboard cảnh báo để đẩy Sale giảm giá hoặc ưu tiên xuất FEFO.
        /// </param>
        /// <param name="isOutOfStock">Lọc các mặt hàng có số dư khả dụng chạm mức 0 hoặc dưới mức an toàn (Safety Stock).</param>
        /// <param name="pageIndex">Trang hiện tại.</param>
        /// <param name="pageSize">Kích thước trang.</param>
        /// <param name="allowedWarehouseIds">[Bảo mật RBAC] Phân quyền hiển thị theo kho.</param>
        Task<PagedResult<InventoryReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            bool? isExpiringSoon,
            bool? isOutOfStock,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Lấy chi tiết số dư của một dòng Tồn kho theo ID.
        /// </summary>
        Task<InventoryReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        #endregion

        #region Core Engine - Biến động Tồn kho (Commands)
        /// <summary>
        /// CỘNG TỒN KHO: Tăng số lượng Khả dụng (QuantityAvailable).
        /// Được gọi bởi IInventoryReceiptService khi Phiếu nhập kho được chốt sổ (Completed) 
        /// hoặc IInventoryTransferService khi hàng điều chuyển đã cập bến đích an toàn.
        /// </summary>
        Task IncreaseAvailableAsync(int warehouseId, int variantId, int batchId, decimal quantity);

        /// <summary>
        /// GIỮ CHỖ TỒN KHO: Khóa một lượng hàng hóa nhất định.
        /// Chuyển [Quantity] từ Hàng Khả dụng (Available) sang Hàng Giữ chỗ (Reserved).
        /// Được gọi ngay lập tức khi Khách hàng đặt Đơn (Sales Order) thành công để đảm bảo 
        /// không có ai khác mua trùng vào phần hàng này.
        /// </summary>
        Task ReserveInventoryAsync(int warehouseId, int variantId, int batchId, decimal quantity);

        /// <summary>
        /// XUẤT KHO THỰC TẾ: Trừ hoàn toàn hàng hóa khỏi kho.
        /// Trừ đi [Quantity] thẳng vào mục Hàng Giữ chỗ (QuantityReserved).
        /// Được gọi bởi IInventoryIssueService khi nhân viên kho đóng gói xong và chốt phiếu xuất (Completed).
        /// </summary>
        Task IssueReservedAsync(int warehouseId, int variantId, int batchId, decimal quantity);

        /// <summary>
        /// XỬ LÝ HÀNG TRẢ VỀ: Nhận lại hàng từ khách bom hoặc hoàn trả.
        /// Tùy theo logic kiểm định (QC), số lượng này thường sẽ được cộng vào [QuantityQC] 
        /// để chờ thủ kho kiểm tra xem hàng còn nguyên vẹn để bán tiếp (chuyển sang Available) 
        /// hay đã bị rách/dập (chuyển sang Damaged).
        /// </summary>
        Task ReceiveCustomerReturnAsync(int warehouseId, int variantId, int batchId, decimal quantity);
        #endregion
    }
}