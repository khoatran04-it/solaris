using backend.DTOs;
using backend.DTOs.CustomerGroupDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Nhóm Khách Hàng (Customer Group / Marketing Tag).
    /// Đảm nhiệm việc định nghĩa và quản lý các thẻ (tags) để phân khúc khách hàng, phục vụ cho các chiến dịch tiếp thị, 
    /// quảng cáo hoặc phân tích dữ liệu (Một khách hàng có thể thuộc nhiều nhóm khác nhau).
    /// </summary>
    public interface ICustomerGroupService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách Nhóm khách hàng (không phân trang).
        /// Hỗ trợ nạp dữ liệu cho giao diện Multi-Select trên Web/App khi quản trị viên muốn gắn thẻ cho khách hàng.
        /// </summary>
        /// <param name="isActiveOnly">Nếu true, chỉ lấy các nhóm đang ở trạng thái Hoạt động (Dùng khi tạo/sửa). Nếu false, lấy tất cả (Dùng cho bộ lọc tìm kiếm).</param>
        Task<IEnumerable<CustomerGroupReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách Nhóm khách hàng có hỗ trợ phân trang, tìm kiếm và lọc dữ liệu.
        /// Dùng để hiển thị lên bảng (DataGrid) của trang Quản lý Danh mục.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Mã hoặc Tên nhóm.</param>
        /// <param name="names">Lọc chính xác theo danh sách tên nhóm (phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang hoạt động, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<CustomerGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một Nhóm khách hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm khách hàng.</param>
        /// <returns>Thông tin DTO của nhóm, hoặc null nếu không tồn tại (hoặc đã bị xóa mềm).</returns>
        Task<CustomerGroupReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một Nhóm khách hàng.
        /// Dữ liệu đầu vào sẽ được xác thực (Ví dụ: Mã nhóm không được trùng lặp) trước khi ghi vào CSDL.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới.</param>
        /// <returns>ID của nhóm vừa được tạo thành công.</returns>
        Task<int> CreateAsync(CustomerGroupCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin Nhóm khách hàng (Tên, Mô tả, Trạng thái...).
        /// </summary>
        /// <param name="id">Mã định danh của nhóm cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy nhóm.</returns>
        Task<bool> UpdateAsync(int id, CustomerGroupUpdateDto dto);

        /// <summary>
        /// Xóa mềm một Nhóm khách hàng.
        /// Ràng buộc nghiệp vụ: Hệ thống phải kiểm tra xem nhóm này có đang chứa khách hàng nào (thông qua bảng trung gian CustomerGroupLink) hay không. 
        /// Nếu có, Service sẽ từ chối xóa để tránh mất mát dữ liệu phân loại của khách hàng.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm khóa của một Nhóm khách hàng.
        /// </summary>
        /// <param name="id">Mã định danh của nhóm cần đổi trạng thái.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}