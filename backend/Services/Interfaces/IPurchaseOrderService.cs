using backend.DTOs;
using backend.DTOs.PurchaseOrderDTOs;

namespace backend.Services.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<PagedResult<PurchaseOrderReadDto>> GetPagedAsync(
            string? search, int? supplierId, int? status, DateTime? startDate, DateTime? endDate, int pageIndex, int pageSize);
        Task<PurchaseOrderReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(PurchaseOrderCreateDto dto);
        Task<bool> UpdateAsync(int id, PurchaseOrderCreateDto dto);
        Task<bool> UpdateStatusAsync(int id, PurchaseOrderUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
