using backend.DTOs;
using backend.DTOs.CustomerDTOs;

namespace backend.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerReadDto>> GetAllListAsync();

        Task<PagedResult<CustomerReadDto>> GetPagedAsync(
            string? search,
            string? customerTypeId,
            string? customerTierId,
            string? customerGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        Task<CustomerReadDto?> GetByIdAsync(int id);

        Task<int> CreateAsync(CustomerCreateDto dto);

        Task<bool> UpdateAsync(int id, CustomerUpdateDto dto);

        Task<bool> DeleteAsync(int id);

        Task<bool> ToggleActiveAsync(int id);
    }
}