using backend.DTOs;
using backend.DTOs.CategoryAttributeDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Cấu hình Thuộc tính cho Danh mục (Category Attribute / EAV Template).
    /// Đóng vai trò là bảng trung gian để quy định: Một danh mục cụ thể sẽ có những thuộc tính đặc thù nào.
    /// </summary>
    public interface ICategoryAttributeService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách cấu hình thuộc tính của các danh mục.
        /// </summary>
        Task<IEnumerable<CategoryAttributeReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách cấu hình thuộc tính có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (theo tên danh mục hoặc tên thuộc tính).</param>
        /// <param name="categoryId">Chuỗi danh sách ID danh mục sản phẩm (có thể phân cách bằng dấu phẩy nếu lọc nhiều).</param>
        /// <param name="attributeDefinitionId">Chuỗi danh sách ID định nghĩa thuộc tính từ Từ điển hệ thống (có thể phân cách bằng dấu phẩy).</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        Task<PagedResult<CategoryAttributeReadDto>> GetPagedAsync(
            string? search,
            string? categoryId,
            string? attributeDefinitionId,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một bản ghi cấu hình thuộc tính theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cấu hình (CategoryAttribute ID).</param>
        Task<CategoryAttributeReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Thiết lập mới cấu hình: Gán một thuộc tính từ Từ điển hệ thống vào một Danh mục sản phẩm.
        /// </summary>
        /// <param name="dto">Dữ liệu cấu hình tạo mới.</param>
        /// <returns>ID của bản ghi cấu hình vừa được tạo.</returns>
        Task<int> CreateAsync(CategoryAttributeCreateDto dto);

        /// <summary>
        /// Cập nhật cấu hình thuộc tính của danh mục (Ví dụ: Thay đổi quy định bắt buộc nhập - IsRequired).
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cấu hình cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, CategoryAttributeUpdateDto dto);

        /// <summary>
        /// Xóa bỏ cấu hình: Hủy gán thuộc tính đặc thù khỏi danh mục sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cấu hình cần xóa.</param>
        Task<bool> DeleteAsync(int id);
        #endregion
    }
}