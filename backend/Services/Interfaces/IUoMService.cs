using backend.DTOs;
using backend.DTOs.UoMDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Đơn vị tính (UoM).
    /// </summary>
    public interface IUoMService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách ĐVT (không phân trang).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các ĐVT đang hoạt động (mặc định là false).</param>
        Task<IEnumerable<UoMReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách ĐVT có hỗ trợ tìm kiếm, lọc theo Nhóm ĐVT, trạng thái, thời gian và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên hoặc mã ĐVT.</param>
        /// <param name="categoryId">Lọc theo mã định danh của Nhóm ĐVT trực thuộc.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<UoMReadDto>> GetPagedAsync(
            string? search,
            int? categoryId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một ĐVT theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của ĐVT.</param>
        Task<UoMReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới ĐVT vào hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới ĐVT.</param>
        /// <returns>ID của ĐVT vừa được tạo.</returns>
        Task<int> CreateAsync(UoMCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin ĐVT.
        /// </summary>
        /// <param name="id">Mã định danh của ĐVT cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, UoMUpdateDto dto);

        /// <summary>
        /// Xóa mềm ĐVT khỏi hệ thống (có kiểm tra ràng buộc BaseUoM, Product, Conversions, v.v.).
        /// </summary>
        /// <param name="id">Mã định danh của ĐVT cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của ĐVT.
        /// </summary>
        /// <param name="id">Mã định danh của ĐVT cần thay đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}