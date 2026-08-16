using backend.DTOs;
using backend.DTOs.CategoryAttributeDTOs;

namespace backend.Services.Interfaces
{
    public interface ICategoryAttributeService
    {
        Task<IEnumerable<CategoryAttributeReadDto>> GetAllListAsync();

        Task<PagedResult<CategoryAttributeReadDto>> GetPagedAsync(
            string? search,
            string? categoryId,
            string? attributeDefinitionId,
            int pageIndex,
            int pageSize
        );

        Task<CategoryAttributeReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(CategoryAttributeCreateDto dto);
        Task<bool> UpdateAsync(int id, CategoryAttributeUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}