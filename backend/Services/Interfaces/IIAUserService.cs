using backend.DTOs;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ quản lý tài khoản và hồ sơ nhân viên trong hệ thống (IAM).
    /// </summary>
    public interface IIAUserService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách nhân viên đang hoạt động (không phân trang).
        /// </summary>
        Task<IEnumerable<IAUserReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách nhân viên có hỗ trợ tìm kiếm, lọc đa tiêu chí và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên, username, email, SĐT hoặc CCCD.</param>
        /// <param name="roleId">Lọc theo ID vai trò.</param>
        /// <param name="warehouseId">Lọc theo ID kho được phân quyền.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<IAUserReadDto>> GetPagedAsync(
            string? search,
            int? roleId,
            int? warehouseId,
            bool? isActive,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên theo ID (kèm Vai trò, Kho và Quyền ngoại lệ).
        /// </summary>
        /// <param name="id">Mã định danh của nhân viên.</param>
        Task<IAUserReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới tài khoản nhân viên, tự động băm mật khẩu và thiết lập phân quyền ban đầu.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới tài khoản.</param>
        /// <returns>ID của tài khoản vừa được tạo.</returns>
        Task<int> CreateAsync(IAUserCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin hồ sơ và đồng bộ lại danh sách vai trò, kho, quyền tùy biến của nhân viên.
        /// </summary>
        /// <param name="id">Mã định danh của nhân viên cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, IAUserUpdateDto dto);

        /// <summary>
        /// Đổi hoặc thiết lập lại mật khẩu đăng nhập cho nhân viên.
        /// </summary>
        /// <param name="id">Mã định danh của nhân viên.</param>
        /// <param name="dto">Mật khẩu mới cần cập nhật.</param>
        Task<bool> ChangePasswordAsync(int id, IAUserChangePasswordDto dto);

        /// <summary>
        /// Xóa mềm tài khoản nhân viên khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh của nhân viên cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (khóa hoặc mở khóa tài khoản).
        /// </summary>
        /// <param name="id">Mã định danh của nhân viên.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}