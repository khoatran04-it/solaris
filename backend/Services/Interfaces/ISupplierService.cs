using backend.DTOs;
using backend.DTOs.SupplierDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Hồ sơ Nhà cung cấp (Supplier).
    /// Cung cấp các thao tác truy vấn nâng cao và xử lý giao dịch an toàn cho chuỗi cung ứng.
    /// </summary>
    public interface ISupplierService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách nhà cung cấp (không phân trang, thường dùng để load dữ liệu cho Dropdown / Combobox).
        /// </summary>
        /// <returns>Danh sách rút gọn các nhà cung cấp.</returns>
        Task<IEnumerable<SupplierReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách nhà cung cấp có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu đa chiều.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (theo mã code, tên, số điện thoại, email...).</param>
        /// <param name="supplierTypeIds">Chuỗi danh sách ID loại nhà cung cấp phân cách bằng dấu phẩy (VD: "1,2,3").</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true: Đang hợp tác, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo hồ sơ.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        /// <returns>Đối tượng phân trang chứa danh sách nhà cung cấp và tổng số lượng bản ghi.</returns>
        Task<PagedResult<SupplierReadDto>> GetPagedAsync(
            string? search,
            string? supplierTypeIds,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một nhà cung cấp theo ID (bao gồm cả danh sách các địa chỉ giao nhận trực thuộc).
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp.</param>
        /// <returns>Thông tin chi tiết nhà cung cấp hoặc null nếu không tồn tại.</returns>
        Task<SupplierReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một hồ sơ nhà cung cấp (hỗ trợ tạo kèm các địa chỉ giao nhận ban đầu nếu có truyền vào).
        /// </summary>
        /// <param name="dto">Dữ liệu khởi tạo nhà cung cấp.</param>
        /// <returns>ID của nhà cung cấp vừa được tạo mới.</returns>
        /// <exception cref="InvalidOperationException">Ném ra khi trùng mã Code hoặc Số điện thoại.</exception>
        Task<int> CreateAsync(SupplierCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin hồ sơ nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        /// <returns>True nếu cập nhật thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy nhà cung cấp.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi mã Code hoặc Số điện thoại bị trùng lặp với đơn vị khác.</exception>
        Task<bool> UpdateAsync(int id, SupplierUpdateDto dto);

        /// <summary>
        /// Xóa mềm một nhà cung cấp (tự động kiểm tra chặt chẽ các ràng buộc dữ liệu về Lô hàng và Đơn đặt hàng - PO trước khi xóa).
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp cần xóa.</param>
        /// <returns>True nếu xóa thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy nhà cung cấp.</exception>
        /// <exception cref="InvalidOperationException">Ném ra khi nhà cung cấp đang có ràng buộc dữ liệu không thể xóa.</exception>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Hoạt động / Tạm khóa) của nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp cần đổi trạng thái.</param>
        /// <returns>True nếu đổi trạng thái thành công.</returns>
        /// <exception cref="KeyNotFoundException">Ném ra khi không tìm thấy nhà cung cấp.</exception>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}