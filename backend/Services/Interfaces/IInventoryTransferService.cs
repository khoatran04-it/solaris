using backend.DTOs;
using backend.DTOs.InventoryTransferDTOs;

namespace backend.Services.Interfaces
{
    public interface IInventoryTransferService
    {
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

        Task<InventoryTransferReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        Task<int> CreateAsync(InventoryTransferCreateDto dto);
        Task<bool> DispatchTransferAsync(int id, int dispatchedById);
        Task<bool> ReceiveTransferAsync(int id, int receivedById);
        Task<bool> CancelTransferAsync(int id, string reason);
    }
}
