using backend.DTOs;
using backend.DTOs.UoMDTOs;

namespace backend.Services.Interfaces
{
    public interface IUoMService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<UoMReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<UoMReadDto>> GetPagedAsync(
            string? search,
            int? categoryId, // Lọc theo nhóm
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<UoMReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(UoMCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, UoMUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);

        // 7. TOGGLE
        Task<bool> ToggleActiveAsync(int id);
    }
}