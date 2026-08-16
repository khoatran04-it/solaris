using backend.DTOs;
using backend.DTOs.InventoryReceiptDTOs;

namespace backend.Services.Interfaces
{
    public interface IInventoryReceiptService
    {
        Task<PagedResult<InventoryReceiptReadDto>> GetPagedAsync(
            string? search, int? warehouseId, int? supplierId, int? status, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize);
        Task<InventoryReceiptReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(InventoryReceiptCreateDto dto);
        Task<bool> CompleteReceiptAsync(int id, int receivedById, string? note);
        Task<bool> CancelReceiptAsync(int id, string reason);
    }
}
