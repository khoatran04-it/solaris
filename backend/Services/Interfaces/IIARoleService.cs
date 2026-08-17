using backend.DTOs;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Vai trò (Roles) và cấu hình phân quyền trong hệ thống.
    /// </summary>
    public interface IIARoleService
    {
        /// <summary>Lấy toàn bộ danh sách vai trò không phân trang</summary>
        Task<IEnumerable<IARoleReadDto>> GetAllListAsync();

        /// <summary>Lấy danh sách vai trò có phân trang và tìm kiếm</summary>
        Task<PagedResult<IARoleReadDto>> GetPagedAsync(string? search, bool? isActive, int pageIndex, int pageSize);

        /// <summary>Lấy thông tin chi tiết vai trò theo ID (kèm danh sách mã quyền)</summary>
        Task<IARoleReadDto> GetByIdAsync(int id);

        /// <summary>Tạo mới vai trò và gán các quyền mặc định</summary>
        Task<int> CreateAsync(IARoleCreateDto dto);

        /// <summary>Cập nhật thông tin vai trò và thiết lập lại danh sách quyền</summary>
        Task<bool> UpdateAsync(int id, IARoleUpdateDto dto);

        /// <summary>Xóa mềm vai trò khỏi hệ thống</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Kích hoạt hoặc tạm khóa vai trò</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}