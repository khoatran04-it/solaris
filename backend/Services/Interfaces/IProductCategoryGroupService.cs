using backend.DTOs;
using backend.DTOs.ProductCategoryGroupDTOs;

namespace backend.Services.Interfaces
{
    public interface IProductCategoryGroupService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<ProductCategoryGroupReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<ProductCategoryGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<ProductCategoryGroupReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(ProductCategoryGroupCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, ProductCategoryGroupUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);
    }
}
