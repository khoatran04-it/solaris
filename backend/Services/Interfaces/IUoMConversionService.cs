using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;

namespace backend.Services.Interfaces
{
    public interface IUoMConversionService
    {
        // 1. GET ALL (Load Dropdown)
        Task<IEnumerable<UoMConversionReadDto>> GetAllListAsync();

        // 2. GET PAGED (Load Table UI, search & filter)
        Task<PagedResult<UoMConversionReadDto>> GetPagedAsync(
            string? search,
            int? productId, // Lọc riêng quy đổi của 1 sản phẩm
            bool? isStandard, // true: Chỉ lấy quy đổi chung, false: Lấy quy đổi riêng biệt
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        // 3. GET BY ID
        Task<UoMConversionReadDto?> GetByIdAsync(int id);

        // 4. CREATE
        Task<int> CreateAsync(UoMConversionCreateDto dto);

        // 5. UPDATE
        Task<bool> UpdateAsync(int id, UoMConversionUpdateDto dto);

        // 6. DELETE
        Task<bool> DeleteAsync(int id);

        // 7. TOGGLE
        Task<bool> ToggleActiveAsync(int id);
    }
}