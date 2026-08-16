using backend.DTOs;
using backend.DTOs.CustomerTierDTOs;

namespace backend.Services.Interfaces
{
    public interface ICustomerTierService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<CustomerTierReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<CustomerTierReadDto>> GetPagedAsync(
            string? search,
            string? names,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<CustomerTierReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(CustomerTierCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, CustomerTierUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);
    }
}