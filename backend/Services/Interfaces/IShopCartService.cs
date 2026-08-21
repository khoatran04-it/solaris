using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    public interface IShopCartService
    {
        Task<ShopCartDto> GetCartAsync(int customerId);
        Task<ShopCartDto> AddItemAsync(int customerId, ShopCartAddDto request);
        Task<ShopCartDto> UpdateItemQuantityAsync(int customerId, int cartItemId, decimal quantity);
        Task<ShopCartDto> RemoveItemAsync(int customerId, int cartItemId);
        Task<ShopCartDto> ClearCartAsync(int customerId);
        Task<ShopCartDto> SyncGuestCartAsync(int customerId, ShopSyncGuestCartDto request);
    }
}
