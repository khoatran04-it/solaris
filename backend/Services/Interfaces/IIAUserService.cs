using backend.DTOs;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    public interface IIAUserService
    {
        Task<IEnumerable<IAUserReadDto>> GetAllListAsync();
        Task<PagedResult<IAUserReadDto>> GetPagedAsync(string? search, int? roleId, int? warehouseId, bool? isActive, int pageIndex, int pageSize);
        Task<IAUserReadDto> GetByIdAsync(int id);

        Task<int> CreateAsync(IAUserCreateDto dto);
        Task<bool> UpdateAsync(int id, IAUserUpdateDto dto);
        Task<bool> ChangePasswordAsync(int id, IAUserChangePasswordDto dto);

        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}