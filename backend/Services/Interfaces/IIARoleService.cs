using backend.DTOs;
using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ quản lý Vai trò (Roles) và ma trận phân quyền trong hệ thống.
    /// </summary>
    public interface IIARoleService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách vai trò đang hoạt động (không phân trang).
        /// </summary>
        Task<IEnumerable<IARoleReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách vai trò có hỗ trợ tìm kiếm, lọc trạng thái và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên hoặc mã vai trò.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<IARoleReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một vai trò theo ID (kèm danh sách ID quyền hạn trực thuộc).
        /// </summary>
        /// <param name="id">Mã định danh của vai trò.</param>
        Task<IARoleReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới vai trò và thiết lập danh sách quyền hạn ban đầu.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới vai trò.</param>
        /// <returns>ID của vai trò vừa được tạo.</returns>
        Task<int> CreateAsync(IARoleCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin vai trò và đồng bộ lại danh sách quyền hạn trực thuộc.
        /// </summary>
        /// <param name="id">Mã định danh của vai trò cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, IARoleUpdateDto dto);

        /// <summary>
        /// Xóa mềm vai trò khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh của vai trò cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của vai trò.
        /// </summary>
        /// <param name="id">Mã định danh của vai trò.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}