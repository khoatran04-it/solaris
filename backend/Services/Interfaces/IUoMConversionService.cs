using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý và tính toán Quy tắc Quy đổi Đơn vị tính (UoM Conversion).
    /// </summary>
    public interface IUoMConversionService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách quy tắc quy đổi (không phân trang).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các quy tắc đang hoạt động (mặc định là false).</param>
        Task<IEnumerable<UoMConversionReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách quy tắc quy đổi có hỗ trợ tìm kiếm, lọc nâng cao, thời gian và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã/Tên ĐVT nguồn, đích hoặc tên sản phẩm).</param>
        /// <param name="productId">Lọc theo mã định danh sản phẩm.</param>
        /// <param name="isStandard">Lọc theo loại quy đổi (true: quy chuẩn hệ thống, false: đặc thù theo sản phẩm).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<UoMConversionReadDto>> GetPagedAsync(
            string? search,
            int? productId,
            bool? isStandard,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một quy tắc quy đổi theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc quy đổi.</param>
        Task<UoMConversionReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới quy tắc quy đổi (có thẩm định kiểm tra vòng lặp vô hạn và tính hợp lệ cùng nhóm ĐVT).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới quy tắc quy đổi.</param>
        /// <returns>ID của quy tắc vừa được tạo.</returns>
        Task<int> CreateAsync(UoMConversionCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin quy tắc quy đổi.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, UoMConversionUpdateDto dto);

        /// <summary>
        /// Xóa mềm quy tắc quy đổi khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của quy tắc quy đổi.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc cần thay đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}