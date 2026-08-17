using backend.DTOs;
using backend.DTOs.CustomerReturnDTOs;

namespace backend.Services.Interfaces
{
    public interface ICustomerReturnService
    {
        Task<PagedResult<CustomerReturnReadDto>> GetPagedAsync(
            string? search,
            int? warehouseId,
            int? status,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        Task<CustomerReturnReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        Task<int> CreateAsync(CustomerReturnCreateDto dto);
        Task<bool> InspectAndCompleteAsync(int id, int receivedById, CustomerReturnInspectionDto dto);
        Task<bool> RejectReturnAsync(int id, string reason);
    }
}
