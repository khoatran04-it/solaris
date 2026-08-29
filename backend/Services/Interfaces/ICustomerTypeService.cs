using backend.DTOs;
using backend.DTOs.CustomerTypeDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Phân loại khách hàng (Customer Type).
    /// Đảm nhiệm các nghiệp vụ định nghĩa và quản lý các tệp khách hàng cố định (Ví dụ: Khách sỉ, Khách lẻ, Đại lý, HORECA) 
    /// phục vụ cho việc thiết lập chính sách bán hàng hoặc bảng giá riêng biệt.
    /// </summary>
    public interface ICustomerTypeService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách Phân loại khách hàng (không phân trang).
        /// Thường được sử dụng để nạp dữ liệu cho các Dropdown/Combobox trên giao diện Quản trị khi nhân viên tạo hoặc cập nhật hồ sơ khách hàng.
        /// </summary>
        Task<IEnumerable<CustomerTypeReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách Phân loại khách hàng có hỗ trợ phân trang, tìm kiếm và lọc dữ liệu.
        /// Thường dùng để hiển thị trên DataGrid của trang Quản lý Danh mục.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Mã hoặc Tên phân loại.</param>
        /// <param name="names">Lọc chính xác theo danh sách tên phân loại (phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang hoạt động, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<CustomerTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một Phân loại khách hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại khách hàng.</param>
        /// <returns>Thông tin DTO của phân loại, hoặc null nếu không tồn tại (hoặc đã bị xóa mềm).</returns>
        Task<CustomerTypeReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một Phân loại khách hàng.
        /// Hệ thống sẽ kiểm tra tính hợp lệ của dữ liệu (Ví dụ: Mã phân loại phải là duy nhất trên toàn hệ thống) trước khi lưu.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới.</param>
        /// <returns>ID của phân loại vừa được tạo thành công.</returns>
        Task<int> CreateAsync(CustomerTypeCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin Phân loại khách hàng.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy phân loại.</returns>
        Task<bool> UpdateAsync(int id, CustomerTypeUpdateDto dto);

        /// <summary>
        /// Xóa mềm một Phân loại khách hàng.
        /// Ràng buộc nghiệp vụ: Hệ thống phải tự động kiểm tra xem có khách hàng nào đang trực thuộc phân loại này không. 
        /// Nếu có, Service sẽ từ chối thao tác xóa (ném ra Exception hoặc trả về false) để bảo toàn tính toàn vẹn của dữ liệu hồ sơ khách hàng.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm khóa của một Phân loại khách hàng.
        /// </summary>
        /// <param name="id">Mã định danh của phân loại cần đổi trạng thái.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}