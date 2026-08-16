using backend.DTOs;
using backend.DTOs.InventoryDTOs;

namespace backend.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseReadDto>> GetAllListAsync();

        // Bổ sung thêm string? province vào hàm Paged
        Task<PagedResult<WarehouseReadDto>> GetPagedAsync(string? search, bool? isActive, string? province, int pageIndex, int pageSize);

        Task<WarehouseReadDto> GetByIdAsync(int id);
        Task<int> CreateAsync(WarehouseCreateDto dto);
        Task<bool> UpdateAsync(int id, WarehouseUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}