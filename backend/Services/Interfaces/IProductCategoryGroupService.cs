using backend.DTOs;
using backend.DTOs.ProductCategoryGroupDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Nhóm Ngành Hàng lớn (Product Category Group).
    /// </summary>
    public interface IProductCategoryGroupService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách nhóm ngành hàng (không phân trang, thường dùng cho giao diện Dropdown/Select).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các nhóm ngành hàng đang ở trạng thái hoạt động (mặc định là false).</param>
        Task<IEnumerable<ProductCategoryGroupReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách nhóm ngành hàng có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã hoặc tên nhóm ngành hàng.</param>
        /// <param name="names">Lọc theo một danh sách tên cụ thể (có thể phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true: Đang hoạt động, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        Task<PagedResult<ProductCategoryGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một nhóm ngành hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng.</param>
        Task<ProductCategoryGroupReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một nhóm ngành hàng vào hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới nhóm ngành hàng.</param>
        /// <returns>ID của nhóm ngành hàng vừa được tạo.</returns>
        Task<int> CreateAsync(ProductCategoryGroupCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin của một nhóm ngành hàng.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, ProductCategoryGroupUpdateDto dto);

        /// <summary>
        /// Xóa mềm một nhóm ngành hàng (hệ thống sẽ kiểm tra ràng buộc các Danh mục con - ProductCategory trực thuộc trước khi xóa).
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang hoạt động / Tạm khóa) của nhóm ngành hàng.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm ngành hàng cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}