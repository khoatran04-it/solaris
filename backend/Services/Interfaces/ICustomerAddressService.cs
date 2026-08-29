using backend.DTOs.CustomerAddressDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Sổ địa chỉ giao hàng của Khách hàng (Customer Address).
    /// Đảm nhiệm việc lưu trữ các điểm nhận hàng, quản lý trạng thái mặc định và hỗ trợ lưu tọa độ GPS phục vụ thuật toán định tuyến.
    /// </summary>
    public interface ICustomerAddressService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách sổ địa chỉ của một khách hàng cụ thể.
        /// Thường được gọi khi khách hàng vào trang "Quản lý tài khoản" hoặc ở bước "Thanh toán" (Checkout) để chọn địa chỉ nhận hàng.
        /// </summary>
        /// <param name="customerId">Mã định danh của khách hàng.</param>
        /// <returns>Danh sách các địa chỉ giao hàng của khách hàng này.</returns>
        Task<IEnumerable<CustomerAddressReadDto>> GetByCustomerIdAsync(int customerId);

        /// <summary>
        /// Lấy thông tin chi tiết của một địa chỉ cụ thể theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ.</param>
        /// <returns>Thông tin DTO của địa chỉ, hoặc null nếu không tồn tại.</returns>
        Task<CustomerAddressReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Thêm mới một địa chỉ giao hàng vào sổ địa chỉ của khách hàng.
        /// Nghiệp vụ: Nếu đây là địa chỉ đầu tiên được tạo, hệ thống nên tự động thiết lập nó làm địa chỉ mặc định (IsDefault = true).
        /// </summary>
        /// <param name="customerId">Mã định danh của khách hàng sở hữu địa chỉ.</param>
        /// <param name="dto">Dữ liệu địa chỉ mới.</param>
        /// <returns>ID của địa chỉ vừa được tạo thành công.</returns>
        Task<int> CreateAsync(int customerId, CustomerAddressCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin địa chỉ (Người nhận, Số điện thoại, Tọa độ GPS...).
        /// Nghiệp vụ: Tuyệt đối không cho phép thay đổi chủ sở hữu (CustomerId) của địa chỉ này để tránh rò rỉ hoặc sai lệch dữ liệu chéo.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy.</returns>
        Task<bool> UpdateAsync(int id, CustomerAddressUpdateDto dto);

        /// <summary>
        /// Xóa (mềm) một địa chỉ khỏi sổ địa chỉ.
        /// Nghiệp vụ: Dù bị xóa, dữ liệu vẫn phải được giữ lại trong database (IsDeleted = true) để không làm gãy (break) thông tin giao hàng của các Đơn hàng (Orders) lịch sử đã hoàn tất.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chỉ định một địa chỉ cụ thể làm Địa chỉ mặc định.
        /// Nghiệp vụ: Khi thiết lập địa chỉ này làm mặc định, hệ thống phải tự động gỡ cờ mặc định (IsDefault = false) của tất cả các địa chỉ khác thuộc cùng khách hàng này trong cùng một Transaction.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ sẽ được làm mặc định.</param>
        /// <param name="customerId">Mã định danh của khách hàng để đảm bảo tính sở hữu.</param>
        /// <returns>True nếu thiết lập thành công.</returns>
        Task<bool> SetDefaultAsync(int id, int customerId);
        #endregion
    }
}