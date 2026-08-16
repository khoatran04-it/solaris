using backend.DTOs;
using backend.DTOs.CustomerTypeDTOs;

namespace backend.Services.Interfaces
{
    public interface ICustomerTypeService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<CustomerTypeReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<CustomerTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<CustomerTypeReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(CustomerTypeCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, CustomerTypeUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);
    }
}