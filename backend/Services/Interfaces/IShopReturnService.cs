using backend.DTOs;
using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    public interface IShopReturnService
    {
        Task<ShopReturnReadDto> CreateReturnRequestAsync(int customerId, ShopReturnCreateRequestDto request);
        Task<PagedResult<ShopReturnReadDto>> GetCustomerReturnsAsync(int customerId, int pageIndex = 1, int pageSize = 10);
        Task<ShopReturnReadDto?> GetReturnByCodeAsync(int customerId, string returnCode);
    }
}
