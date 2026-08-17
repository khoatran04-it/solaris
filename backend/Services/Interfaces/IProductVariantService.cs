using backend.DTOs;
using backend.DTOs.ProductVariantDTOs;

namespace backend.Services.Interfaces
{
    public interface IProductVariantService
    {
        Task<IEnumerable<ProductVariantReadDto>> GetAllListAsync(bool isActiveOnly = false);

        Task<PagedResult<ProductVariantReadDto>> GetPagedAsync(
            string? search,
            string? productId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        Task<ProductVariantReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductVariantCreateDto dto);
        Task<bool> UpdateAsync(int id, ProductVariantUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}