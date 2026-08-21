using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    public interface IShopCustomerService
    {
        Task<ShopCustomerProfileDto> GetProfileAsync(int customerId);
        Task<ShopCustomerProfileDto> UpdateProfileAsync(int customerId, ShopCustomerProfileUpdateDto request);
        Task<List<ShopAddressDto>> GetAddressesAsync(int customerId);
        Task<ShopAddressDto> AddAddressAsync(int customerId, ShopAddressCreateDto request);
        Task<ShopAddressDto> UpdateAddressAsync(int customerId, int addressId, ShopAddressUpdateDto request);
        Task<bool> DeleteAddressAsync(int customerId, int addressId);
        Task<bool> SetDefaultAddressAsync(int customerId, int addressId);
    }
}
