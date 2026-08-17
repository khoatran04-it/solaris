using backend.DTOs;
using backend.DTOs.OrderDTOs;

namespace backend.Services.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResult<OrderReadDto>> GetPagedAsync(
            string? search,
            int? customerId,
            int? warehouseId,
            int? status,
            int? paymentStatus,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        Task<OrderReadDto> GetByIdAsync(int id, List<int>? allowedWarehouseIds = null);
        Task<int> CreateAsync(OrderCreateDto dto);
        Task<bool> UpdateStatusAsync(int id, OrderUpdateDto dto);
        Task<bool> CancelAsync(int id, string reason);
    }
}
