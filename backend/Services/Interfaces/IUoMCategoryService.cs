using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ quản lý Nhóm Đơn vị tính (UoM Category).
    /// </summary>
    public interface IUoMCategoryService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách nhóm ĐVT (không phân trang).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các nhóm đang hoạt động (mặc định là false).</param>
        Task<IEnumerable<UoMCategoryReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách nhóm ĐVT có hỗ trợ tìm kiếm, lọc trạng thái, thời gian và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên hoặc mã nhóm ĐVT.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<UoMCategoryReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một nhóm ĐVT theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ĐVT.</param>
        Task<UoMCategoryReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới nhóm ĐVT.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới nhóm ĐVT.</param>
        /// <returns>ID của nhóm ĐVT vừa được tạo.</returns>
        Task<int> CreateAsync(UoMCategoryCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin nhóm ĐVT.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ĐVT cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, UoMCategoryUpdateDto dto);

        /// <summary>
        /// Xóa mềm nhóm ĐVT khỏi hệ thống (có kiểm tra ràng buộc đơn vị tính con).
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ĐVT cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của nhóm ĐVT.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ĐVT.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}