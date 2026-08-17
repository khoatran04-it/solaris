using backend.DTOs;
using backend.DTOs.AttributeDefinitionDTOs;

namespace backend.Services.Interfaces
{
    public interface IAttributeDefinitionService
    {
        Task<IEnumerable<AttributeDefinitionReadDto>> GetAllListAsync(bool isActiveOnly = false);

        Task<PagedResult<AttributeDefinitionReadDto>> GetPagedAsync(
            string? search,
            string? dataType,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        Task<AttributeDefinitionReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(AttributeDefinitionCreateDto dto);
        Task<bool> UpdateAsync(int id, AttributeDefinitionUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}