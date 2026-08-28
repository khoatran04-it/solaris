using backend.DTOs;
using backend.DTOs.AttributeDefinitionDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Từ điển Thuộc tính (Attribute Definition).
    /// Trung tâm định nghĩa các thuộc tính động (EAV) cho toàn bộ hệ thống (Ví dụ: Độ ngọt, Xuất xứ, Khối lượng).
    /// </summary>
    public interface IAttributeDefinitionService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách từ điển thuộc tính (không phân trang, dùng để load dữ liệu cho Dropdown/Select khi cấu hình danh mục).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các thuộc tính đang ở trạng thái hoạt động (mặc định là false).</param>
        Task<IEnumerable<AttributeDefinitionReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách từ điển thuộc tính có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên thuộc tính.</param>
        /// <param name="dataType">Lọc theo kiểu dữ liệu của thuộc tính (Ví dụ: String, Number, Boolean).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true: Đang hoạt động, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        Task<PagedResult<AttributeDefinitionReadDto>> GetPagedAsync(
            string? search,
            string? dataType,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một định nghĩa thuộc tính theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của định nghĩa thuộc tính trong từ điển.</param>
        Task<AttributeDefinitionReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một định nghĩa thuộc tính vào từ điển hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu khởi tạo thuộc tính.</param>
        /// <returns>ID của định nghĩa thuộc tính vừa được tạo.</returns>
        Task<int> CreateAsync(AttributeDefinitionCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin của một định nghĩa thuộc tính.
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, AttributeDefinitionUpdateDto dto);

        /// <summary>
        /// Xóa mềm một định nghĩa thuộc tính (Hệ thống sẽ kiểm tra xem thuộc tính này có đang được gán cho Danh mục hoặc Sản phẩm nào không trước khi xóa).
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang hoạt động / Tạm khóa) của định nghĩa thuộc tính.
        /// </summary>
        /// <param name="id">Mã định danh của thuộc tính cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}