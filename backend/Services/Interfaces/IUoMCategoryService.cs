using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;

namespace backend.Services.Interfaces
{
    public interface IUoMCategoryService
    {
        //1. GET ALL
        Task<IEnumerable<UoMCategoryReadDto>> GetAllListAsync();

        //2. GET PAGED 
        Task<PagedResult<UoMCategoryReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        //3. GET BY ID
        Task<UoMCategoryReadDto?> GetByIdAsync(int id);

        //4. CREATE
        Task<int> CreateAsync(UoMCategoryCreateDto dto);

        //5. UPDATE
        Task<bool> UpdateAsync(int id, UoMCategoryUpdateDto dto);

        //6. DELETE
        Task<bool> DeleteAsync (int id);

        //7. TOGGLE
        Task<bool> ToggleActiveAsync(int id);
    }
}
