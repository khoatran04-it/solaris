using backend.DTOs;
using backend.DTOs.SupplierProductDTOs;

namespace backend.Services.Interfaces
{
    public interface ISupplierProductService
    {
        Task<IEnumerable<SupplierProductReadDto>> GetAllListAsync();

        Task<PagedResult<SupplierProductReadDto>> GetPagedAsync(
            string? search, // Tìm theo mã SupplierSKU
            int? variantId, // Lọc theo Biến thể
            int? supplierId, // Lọc theo Nhà cung cấp
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        Task<SupplierProductReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(SupplierProductCreateDto dto);
        Task<bool> UpdateAsync(int id, SupplierProductUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}