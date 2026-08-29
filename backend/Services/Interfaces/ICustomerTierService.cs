using backend.DTOs;
using backend.DTOs.CustomerTierDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Hạng / Bậc Khách Hàng (Customer Loyalty Tier).
    /// Đảm nhiệm các nghiệp vụ thiết lập chính sách khách hàng thân thiết, quy định mức chiết khấu và điều kiện thăng hạng.
    /// </summary>
    public interface ICustomerTierService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách Hạng khách hàng (không phân trang).
        /// Thường được sử dụng để nạp dữ liệu cho các Dropdown/Combobox trên giao diện Quản trị khi gán hạng thủ công cho khách hàng, hoặc dùng trong bộ lọc.
        /// </summary>
        Task<IEnumerable<CustomerTierReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách Hạng khách hàng có hỗ trợ phân trang, tìm kiếm và lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Mã hoặc Tên hạng.</param>
        /// <param name="names">Lọc chính xác theo danh sách tên hạng (phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang áp dụng, false: Tạm ngưng).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<CustomerTierReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một Hạng khách hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của hạng khách hàng.</param>
        /// <returns>Thông tin DTO của hạng, hoặc null nếu không tồn tại (hoặc đã bị xóa mềm).</returns>
        Task<CustomerTierReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một chính sách Hạng khách hàng.
        /// Hệ thống sẽ kiểm tra tính hợp lệ của dữ liệu (ví dụ: Mã hạng không được trùng lặp) trước khi lưu.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới.</param>
        /// <returns>ID của hạng vừa được tạo thành công.</returns>
        Task<int> CreateAsync(CustomerTierCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin Hạng khách hàng (Thay đổi % chiết khấu, hạn mức chi tiêu...).
        /// </summary>
        /// <param name="id">Mã định danh của hạng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy.</returns>
        Task<bool> UpdateAsync(int id, CustomerTierUpdateDto dto);

        /// <summary>
        /// Xóa mềm một Hạng khách hàng.
        /// Đảm bảo nghiệp vụ chặt chẽ: Dịch vụ sẽ tự động kiểm tra xem có khách hàng nào đang ở hạng này không. Nếu có, sẽ chặn việc xóa (ném ra Exception hoặc trả về false).
        /// </summary>
        /// <param name="id">Mã định danh của hạng cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm ngưng áp dụng của một Hạng khách hàng.
        /// </summary>
        /// <param name="id">Mã định danh của hạng cần đổi trạng thái.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}