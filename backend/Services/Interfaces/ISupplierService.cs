using backend.DTOs;
using backend.DTOs.SupplierDTOs;

namespace backend.Services.Interfaces
{
    public interface ISupplierService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<SupplierReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<SupplierReadDto>> GetPagedAsync(
            string? search,
            string? supplierTypesId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<SupplierReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(SupplierCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, SupplierUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);

        //7. TOGGLE
        Task<bool> ToggleActiveAsync(int id);
    }
}