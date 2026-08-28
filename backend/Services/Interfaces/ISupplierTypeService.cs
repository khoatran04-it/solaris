using backend.DTOs;
using backend.DTOs.SupplierTypeDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Phân loại Nhà cung cấp (Supplier Type).
    /// </summary>
    public interface ISupplierTypeService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách phân loại (không phân trang, phục vụ hiển thị Dropdown / Select).
        /// </summary>
        Task<IEnumerable<SupplierTypeReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách phân loại nhà cung cấp có hỗ trợ phân trang, tìm kiếm và lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã hoặc tên phân loại.</param>
        /// <param name="names">Lọc theo một danh sách tên cụ thể (có thể phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật cuối.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<SupplierTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một phân loại nhà cung cấp theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại nhà cung cấp.</param>
        Task<SupplierTypeReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một phân loại nhà cung cấp vào hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới phân loại.</param>
        /// <returns>ID của phân loại vừa được tạo.</returns>
        Task<int> CreateAsync(SupplierTypeCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin phân loại nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, SupplierTypeUpdateDto dto);

        /// <summary>
        /// Xóa mềm phân loại nhà cung cấp khỏi hệ thống (có kiểm tra ràng buộc dữ liệu nhà cung cấp trực thuộc).
        /// </summary>
        /// <param name="id">Mã định danh của phân loại cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Hoạt động / Tạm khóa) của phân loại nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}