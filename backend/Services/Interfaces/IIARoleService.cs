using backend.DTOs;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    public interface IIARoleService
    {
        Task<IEnumerable<IARoleReadDto>> GetAllListAsync();
        Task<PagedResult<IARoleReadDto>> GetPagedAsync(string? search, bool? isActive, int pageIndex, int pageSize);
        Task<IARoleReadDto> GetByIdAsync(int id);

        Task<int> CreateAsync(IARoleCreateDto dto);
        Task<bool> UpdateAsync(int id, IARoleUpdateDto dto);

        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}