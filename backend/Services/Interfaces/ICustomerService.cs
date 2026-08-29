using backend.DTOs;
using backend.DTOs.CustomerDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service trung tâm quản lý Khách Hàng (Customer).
    /// Chịu trách nhiệm xử lý toàn bộ vòng đời hồ sơ khách hàng, điều phối liên kết với Phân loại (Type), Hạng (Tier), Nhóm tiếp thị (Groups) và Sổ địa chỉ (Addresses).
    /// </summary>
    public interface ICustomerService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách Khách hàng (không phân trang).
        /// Thường dùng cho các tính năng Lookup (Tìm kiếm nhanh / Dropdown) khi nhân viên tiến hành tạo Đơn hàng (Order) tại quầy hoặc trên hệ thống.
        /// </summary>
        Task<IEnumerable<CustomerReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách Khách hàng có hỗ trợ phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm đa năng (Mã khách hàng, Tên, Số điện thoại, Email).</param>
        /// <param name="customerTypeId">Lọc theo danh sách ID Phân loại (Hỗ trợ truyền chuỗi phân cách bằng dấu phẩy, VD: "1,2,3").</param>
        /// <param name="customerTierId">Lọc theo danh sách ID Bậc hạng (Hỗ trợ truyền chuỗi phân cách bằng dấu phẩy).</param>
        /// <param name="customerGroupId">Lọc theo danh sách ID Nhóm tiếp thị (Hỗ trợ truyền chuỗi phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái tài khoản.</param>
        /// <param name="createdAt">Lọc theo ngày đăng ký/tạo hồ sơ.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<CustomerReadDto>> GetPagedAsync(
            string? search,
            string? customerTypeId,
            string? customerTierId,
            string? customerGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết toàn diện của một Khách hàng theo ID.
        /// Nghiệp vụ: Trả về bao gồm cả thông tin mở rộng (Enriched data) như Tên hạng, Tên phân loại, Danh sách địa chỉ và các Nhóm đang tham gia.
        /// </summary>
        /// <param name="id">Mã định danh của khách hàng.</param>
        /// <returns>Thông tin DTO chi tiết, hoặc null nếu không tồn tại.</returns>
        Task<CustomerReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới hồ sơ Khách hàng.
        /// Nghiệp vụ phức hợp (Complex Transaction): Nếu DTO có truyền kèm danh sách Nhóm (GroupIds) và Sổ địa chỉ (Addresses), 
        /// Service phải tự động lưu liên kết nhóm và khởi tạo địa chỉ mặc định đầu tiên trong cùng một DB Transaction để đảm bảo tính toàn vẹn dữ liệu.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới.</param>
        /// <returns>ID của khách hàng vừa được tạo thành công.</returns>
        Task<int> CreateAsync(CustomerCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin hồ sơ Khách hàng.
        /// Nghiệp vụ: Cập nhật thông tin cơ bản và điều chỉnh lại danh sách Nhóm tiếp thị (Thực hiện thuật toán xóa liên kết cũ, thêm liên kết mới). 
        /// Không can thiệp cập nhật Sổ địa chỉ qua API này.
        /// </summary>
        /// <param name="id">Mã định danh của khách hàng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy hồ sơ.</returns>
        Task<bool> UpdateAsync(int id, CustomerUpdateDto dto);

        /// <summary>
        /// Xóa mềm hồ sơ Khách hàng.
        /// Ràng buộc nghiệp vụ ngặt nghèo: Phải kiểm tra khách hàng đã có phát sinh giao dịch nào chưa (Đơn hàng, Phiếu trả hàng, Hóa đơn, Công nợ...). 
        /// Nếu có bất kỳ dữ liệu tài chính/chứng từ nào liên quan, tuyệt đối chặn hành vi xóa để bảo vệ an toàn kế toán.
        /// </summary>
        /// <param name="id">Mã định danh của khách hàng cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm khóa của tài khoản Khách hàng.
        /// Nghiệp vụ: Khi bị khóa, khách hàng sẽ không thể đăng nhập (Web/App) hoặc nhân viên không thể chọn khách hàng này để chốt đơn mới.
        /// </summary>
        /// <param name="id">Mã định danh của khách hàng cần thao tác.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}