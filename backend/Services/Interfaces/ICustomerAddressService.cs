using backend.DTOs.CustomerAddressDTOs;

namespace backend.Services.Interfaces
{
    public interface ICustomerAddressService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<CustomerAddressReadDto>> GetByCustomerIdAsync(int customerId);

        // 2. GET BY ID
        Task<CustomerAddressReadDto?> GetByIdAsync(int id);

        // 3. CREATE
        Task<int> CreateAsync(int customerId, CustomerAddressCreateDto dto);

        // 4. UPDATE
        Task<bool> UpdateAsync(int id, CustomerAddressUpdateDto dto);

        // 5. DELETE
        Task<bool> DeleteAsync(int id);

        // 6. SET DEFAULT
        Task<bool> SetDefaultAsync(int id, int customerId);
    }
}