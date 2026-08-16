using backend.DTOs;
using backend.DTOs.SupplierTypeDTOs;

namespace backend.Services.Interfaces
{
    public interface ISupplierTypeService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<SupplierTypeReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<SupplierTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<SupplierTypeReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(SupplierTypeCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, SupplierTypeUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);
    }
}