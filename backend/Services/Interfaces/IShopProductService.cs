using backend.DTOs;
using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    public interface IShopProductService
    {
        Task<PagedResult<ShopProductCardDto>> GetProductsAsync(ShopProductFilterParams filter);
        Task<ShopProductDetailDto?> GetProductBySlugAsync(string slug);
        Task<List<ShopCategoryTreeDto>> GetCategoryTreeAsync();
        Task<List<ShopProductCardDto>> GetFeaturedProductsAsync(int limit = 8);
        Task<List<ShopProductCardDto>> GetNewArrivalsAsync(int limit = 8);
        Task<List<ShopPromotionBadgeDto>> GetActivePromotionsAsync();
        Task<ShopPromotionDetailDto?> GetPromotionBySlugAsync(string slug);
        Task<List<string>> GetAvailableOriginsAsync();
        Task<List<string>> GetAvailableCertificationsAsync();
    }
}
