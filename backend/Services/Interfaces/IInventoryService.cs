using backend.DTOs;
using backend.DTOs.InventoryDTOs;

namespace backend.Services.Interfaces
{
    public interface IInventoryService
    {
        // =========================================================================
        // PHẦN 1: CÁC HÀM ĐỌC DỮ LIỆU (Có kèm List<int>? allowedWarehouseIds để bảo mật)
        // =========================================================================

        Task<IEnumerable<InventoryReadDto>> GetAllListAsync(List<int>? allowedWarehouseIds = null);

        Task<PagedResult<InventoryReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            bool? isExpiringSoon,
            bool? isOutOfStock,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null // <--- Chìa khóa phân quyền ở đây
        );

        Task<InventoryReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);


        // =========================================================================
        // PHẦN 2: CÁC HÀM CORE ENGINE (Dành cho Module Nhập/Xuất gọi vào)
        // =========================================================================

        Task IncreaseAvailableAsync(int warehouseId, int variantId, int batchId, decimal quantity);

        Task ReserveInventoryAsync(int warehouseId, int variantId, int batchId, decimal quantity);

        Task IssueReservedAsync(int warehouseId, int variantId, int batchId, decimal quantity);

        Task ReceiveCustomerReturnAsync(int warehouseId, int variantId, int batchId, decimal quantity);
    }
}