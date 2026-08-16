using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;

namespace backend.Services.Interfaces
{
    public interface IProductCategoryService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<ProductCategoryReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<ProductCategoryReadDto>> GetPagedAsync(
            string? search,
            string? names,
            string? categoryGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<ProductCategoryReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(ProductCategoryCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, ProductCategoryUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);
    }
}
