using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public interface IProductCategoryService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách danh mục sản phẩm (không phân trang, thường dùng cho giao diện Dropdown/Select).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các danh mục đang ở trạng thái hoạt động (mặc định là false).</param>
        Task<IEnumerable<ProductCategoryReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách danh mục sản phẩm có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu đa chiều.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã hoặc tên danh mục.</param>
        /// <param name="names">Lọc theo một danh sách tên cụ thể (có thể phân cách bằng dấu phẩy).</param>
        /// <param name="categoryGroupId">Chuỗi danh sách ID nhóm ngành hàng lớn (có thể phân cách bằng dấu phẩy nếu lọc nhiều nhóm).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true: Đang hoạt động, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        Task<PagedResult<ProductCategoryReadDto>> GetPagedAsync(
            string? search,
            string? names,
            string? categoryGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một danh mục sản phẩm theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của danh mục.</param>
        Task<ProductCategoryReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một danh mục sản phẩm vào hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới danh mục.</param>
        /// <returns>ID của danh mục vừa được tạo.</returns>
        Task<int> CreateAsync(ProductCategoryCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin của một danh mục sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của danh mục cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, ProductCategoryUpdateDto dto);

        /// <summary>
        /// Xóa mềm một danh mục sản phẩm (hệ thống sẽ tự động kiểm tra ràng buộc các Sản phẩm - Product trực thuộc trước khi xóa).
        /// </summary>
        /// <param name="id">Mã định danh của danh mục cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang hoạt động / Tạm khóa) của danh mục sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của danh mục cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}