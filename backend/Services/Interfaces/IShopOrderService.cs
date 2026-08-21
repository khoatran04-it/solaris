using backend.DTOs;
using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    public interface IShopOrderService
    {
        Task<ShopOrderReadDto> CheckoutAsync(int customerId, ShopCheckoutRequestDto request);
        Task<PagedResult<ShopOrderReadDto>> GetCustomerOrdersAsync(int customerId, int pageIndex = 1, int pageSize = 10);
        Task<ShopOrderReadDto?> GetOrderByCodeAsync(int customerId, string orderCode);
        Task<bool> CancelOrderAsync(int customerId, string orderCode, string reason);
    }
}
