using backend.DTOs.SupplierAddressDTOs;

namespace backend.Services.Interfaces
{
    public interface ISupplierAddressService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<SupplierAddressReadDto>> GetBySupplierIdAsync(int SupplierId);

        Task<SupplierAddressReadDto?> GetByIdAsync(int id);

        // 2. CREATE
        Task<int> CreateAsync(int SupplierId, SupplierAddressCreateDto dto);

        // 3. UPDATE
        Task<bool> UpdateAsync(int id, SupplierAddressUpdateDto dto);

        // 4. DELETE
        Task<bool> DeleteAsync(int id);

        Task<bool> SetDefaultAsync(int id, int SupplierId);
    }
}