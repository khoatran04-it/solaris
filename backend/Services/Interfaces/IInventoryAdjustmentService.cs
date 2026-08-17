using backend.DTOs;
using backend.DTOs.InventoryAdjustmentDTOs;

namespace backend.Services.Interfaces
{
    public interface IInventoryAdjustmentService
    {
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

        Task<InventoryAdjustmentReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        Task<int> CreateAsync(InventoryAdjustmentCreateDto dto);
        Task<bool> ApproveAdjustmentAsync(int id, int approvedById);
        Task<bool> CancelAsync(int id, string reason);
    }
}
