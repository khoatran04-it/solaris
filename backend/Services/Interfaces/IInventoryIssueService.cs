using backend.DTOs;
using backend.DTOs.InventoryIssueDTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// DTO Trả về kết quả của thuật toán Gợi ý xuất kho (Smart FEFO Picker).
    /// Giúp nhân viên kho biết chính xác cần nhặt bao nhiêu hàng từ lô nào để tối ưu hóa vòng đời nông sản.
    /// </summary>
    public class SuggestedBatchDto
    {
        public int BatchId { get; set; }

        /// <summary>Mã Lô hàng (Để nhân viên đối chiếu với mã vạch trên thùng hàng thực tế).</summary>
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>Ngày hết hạn (Thuật toán FEFO sẽ ưu tiên xếp các lô cận date lên đầu danh sách).</summary>
        public DateTime? ExpiryDate { get; set; }

        public decimal QuantityAvailable { get; set; }
        public decimal QuantityReserved { get; set; }

        /// <summary>
        /// Số lượng hệ thống đề xuất nhặt từ Lô này. 
        /// (Ví dụ: Khách mua 100kg, nhưng Lô A cận date nhất chỉ còn 30kg -> Suggested Lô A = 30kg, Lô B = 70kg).
        /// </summary>
        public decimal SuggestedPickQuantity { get; set; }
    }

    /// <summary>
    /// Giao diện Service quản lý Phiếu Xuất Kho (Inventory Issue / Goods Issue Note).
    /// Xử lý quy trình: Nhặt hàng theo lô FEFO -> Đóng gói -> Xuất chốt sổ trừ tồn kho.
    /// </summary>
    public interface IInventoryIssueService
    {
        #region Truy vấn & Phân quyền (Query & RBAC)
        /// <summary>
        /// Lấy danh sách Phiếu xuất kho kèm phân trang và bộ lọc.
        /// </summary>
        /// <param name="allowedWarehouseIds">
        /// [Bảo mật RBAC] Danh sách ID các kho mà User hiện tại được phép truy cập. 
        /// Truyền null nếu là SuperAdmin (Xem toàn cục).
        /// </param>
        Task<PagedResult<InventoryIssueReadDto>> GetPagedAsync(
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
        /// Lấy chi tiết Phiếu xuất kho theo ID (Có kiểm tra quyền truy cập kho).
        /// </summary>
        Task<InventoryIssueReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        #endregion

        #region Thao tác Dữ liệu & Quy trình (Command & Workflow)
        /// <summary>
        /// Lập phiếu xuất kho mới.
        /// Tùy theo logic nghiệp vụ, có thể kích hoạt cơ chế Khóa tồn kho (Reserve Stock) ngay tại bước này.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo phiếu.</param>
        /// <param name="currentUserId">ID người tạo phiếu (Lấy từ JWT Token).</param>
        Task<int> CreateAsync(InventoryIssueCreateDto dto, int? currentUserId = null);

        /// <summary>
        /// NGHIỆP VỤ LÕI: Hoàn tất Xuất kho (Chốt sổ / Ship Confirm).
        /// Mở Transaction (Unit of Work) thực hiện:
        /// 1. Gọi IInventoryService để TRỪ số dư Tồn kho thực tế (QuantityAvailable).
        /// 2. Giải phóng số lượng đã giữ chỗ (QuantityReserved) của Đơn hàng tương ứng (nếu có).
        /// 3. Cập nhật Status phiếu thành Completed.
        /// </summary>
        Task<bool> CompleteIssueAsync(int id, int issuedById, string? note);

        /// <summary>
        /// Hủy phiếu xuất kho. Yêu cầu nhập lý do hủy.
        /// Nếu phiếu đang giữ chỗ tồn kho (Reserved), Service sẽ giải phóng số dư này trả lại cho hệ thống.
        /// </summary>
        Task<bool> CancelIssueAsync(int id, string reason);

        /// <summary>
        /// Xóa phiếu xuất kho (Soft Delete).
        /// Nghiệp vụ chặn xóa: Không được phép xóa phiếu đã Completed (đã làm thay đổi sổ cái kế toán kho).
        /// </summary>
        Task<bool> DeleteAsync(int id);
        #endregion

        #region Thuật toán Kho (Smart Logistics)
        /// <summary>
        /// Thuật toán FEFO (First Expired, First Out - Hết hạn trước, xuất trước).
        /// Truy vấn và đề xuất danh sách Lô hàng cần lấy để đáp ứng đủ số lượng (neededQuantity) của một Sản phẩm.
        /// </summary>
        /// <param name="warehouseId">Kho đang thao tác.</param>
        /// <param name="variantId">ID Sản phẩm cần lấy.</param>
        /// <param name="neededQuantity">Tổng số lượng cần xuất.</param>
        /// <returns>Danh sách các Lô hàng được gợi ý kèm theo số lượng cụ thể cần bốc từ từng Lô.</returns>
        Task<List<SuggestedBatchDto>> GetSuggestedBatchesAsync(int warehouseId, int variantId, decimal neededQuantity);
        #endregion
    }
}