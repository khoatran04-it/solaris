using backend.DTOs;
using backend.DTOs.SupplierProductDTOs;

namespace backend.Services.Interfaces
{
    public interface ISupplierProductService
    {
        Task<IEnumerable<SupplierProductReadDto>> GetAllListAsync(bool isActiveOnly = false);

        Task<IEnumerable<SupplierProductReadDto>> GetBySupplierIdAsync(int supplierId, bool isActiveOnly = true);

        Task<PagedResult<SupplierProductReadDto>> GetPagedAsync(
            string? search,
            int? variantId,
            int? supplierId,
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