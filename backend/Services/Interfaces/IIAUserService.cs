using backend.DTOs;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý tài khoản và hồ sơ nhân viên trong hệ thống (IAM).
    /// </summary>
    public interface IIAUserService
    {
        /// <summary>Lấy toàn bộ danh sách nhân viên không phân trang</summary>
        Task<IEnumerable<IAUserReadDto>> GetAllListAsync();

        /// <summary>Lấy danh sách nhân viên có phân trang, tìm kiếm và lọc theo Vai trò / Kho</summary>
        Task<PagedResult<IAUserReadDto>> GetPagedAsync(string? search, int? roleId, int? warehouseId, bool? isActive, int pageIndex, int pageSize);

        /// <summary>Lấy thông tin chi tiết nhân viên theo ID (kèm Vai trò, Kho, Quyền tùy biến)</summary>
        Task<IAUserReadDto> GetByIdAsync(int id);

        /// <summary>Tạo mới tài khoản nhân viên (tự động mã hóa mật khẩu và phân quyền)</summary>
        Task<int> CreateAsync(IAUserCreateDto dto);

        /// <summary>Cập nhật thông tin hồ sơ nhân viên</summary>
        Task<bool> UpdateAsync(int id, IAUserUpdateDto dto);

        /// <summary>Đổi hoặc đặt lại mật khẩu nhân viên</summary>
        Task<bool> ChangePasswordAsync(int id, IAUserChangePasswordDto dto);

        /// <summary>Xóa mềm tài khoản nhân viên</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Kích hoạt hoặc tạm khóa tài khoản nhân viên</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}