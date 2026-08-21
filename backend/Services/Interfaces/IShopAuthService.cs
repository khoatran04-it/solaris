using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    public interface IShopAuthService
    {
        Task<ShopAuthResponseDto> RegisterAsync(ShopRegisterRequestDto request);
        Task<ShopAuthResponseDto> LoginAsync(ShopLoginRequestDto request);
        Task<ShopCustomerInfoDto> GetCurrentCustomerAsync(int customerId);
    }
}
