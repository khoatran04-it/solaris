using backend.DTOs;
using backend.DTOs.CustomerGroupDTOs;

namespace backend.Services.Interfaces
{
    public interface ICustomerGroupService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<CustomerGroupReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<CustomerGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive, // Thêm lọc trạng thái
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<CustomerGroupReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(CustomerGroupCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, CustomerGroupUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);

        Task<bool> ToggleActiveAsync(int id);
    }
}