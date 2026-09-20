using backend.DTOs;
using backend.DTOs.InventoryDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Kho Hàng (Warehouse).
    /// Đảm nhiệm vòng đời của các kho vật lý/cửa hàng, làm nền tảng cơ sở dữ liệu cho phân hệ 
    /// Quản lý Tồn kho (Inventory) và Thuật toán Định tuyến Vận chuyển (Smart Routing).
    /// </summary>
    public interface IWarehouseService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách Kho hàng (không phân trang).
        /// Thường dùng cho các tính năng Lookup (Dropdown/Combobox) khi tạo Phiếu nhập/xuất kho, 
        /// điều chuyển hàng hóa, hoặc gán quyền truy cập kho cho nhân viên.
        /// </summary>
        /// <param name="isActiveOnly">Nếu true, chỉ lấy các kho đang hoạt động (Dùng khi tạo phiếu). Nếu false, lấy tất cả (Dùng cho bộ lọc tra cứu).</param>
        /// <param name="warehouseType">Tùy chọn lọc theo loại kho cụ thể (Ví dụ: Kho Tổng, Kho Bán Lẻ).</param>
        Task<IEnumerable<WarehouseReadDto>> GetAllListAsync(bool isActiveOnly = false, string? warehouseType = null, List<int>? allowedWarehouseIds = null);

        /// <summary>
        /// Lấy danh sách Kho hàng có hỗ trợ phân trang và bộ lọc nâng cao.
        /// Thường dùng để hiển thị lên DataGrid trang Quản lý danh sách Kho.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Tên hoặc Mã kho.</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang hoạt động, false: Tạm đóng).</param>
        /// <param name="province">Lọc kho hàng theo Khu vực / Tỉnh thành.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        /// <param name="allowedWarehouseIds">Danh sách các kho được phép truy cập theo quyền người dùng.</param>
        Task<PagedResult<WarehouseReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            string? province,
            int pageIndex,
            int pageSize,
            List<int>? allowedWarehouseIds = null
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một Kho hàng theo ID.
        /// Dữ liệu trả về bao gồm thông tin cơ bản và tọa độ địa lý, hành chính (đã được làm phẳng).
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng.</param>
        /// <returns>Thông tin DTO của kho, hoặc null nếu không tồn tại.</returns>
        Task<WarehouseReadDto?> GetByIdAsync(int id);

        /// <summary>
        /// Lấy thông tin trạng thái sức chứa tức thời (CBM, Tải trọng kg, % lấp đầy) của kho hàng.
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng.</param>
        /// <returns>DTO trạng thái sức chứa.</returns>
        Task<WarehouseCapacityStatusDto> GetCapacityStatusAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một Kho hàng.
        /// Nghiệp vụ phức hợp (Complex Transaction): Service phải tự động tách đối tượng AddressPayload 
        /// để tạo mới bản ghi WarehouseAddress trước, sau đó lấy AddressId gán vào Kho và lưu đồng thời trong 1 Transaction.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới Kho hàng và Địa chỉ.</param>
        /// <returns>ID của kho hàng vừa được tạo thành công.</returns>
        Task<int> CreateAsync(WarehouseCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin Kho hàng.
        /// Nghiệp vụ: Đối với phần Address, Service sẽ tìm bản ghi WarehouseAddress cũ trong Database 
        /// và ghi đè (map) dữ liệu mới vào để không làm thay đổi ID địa chỉ hiện tại.
        /// </summary>
        /// <param name="id">Mã định danh của kho cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy kho.</returns>
        Task<bool> UpdateAsync(int id, WarehouseUpdateDto dto);

        /// <summary>
        /// Xóa (mềm) một Kho hàng.
        /// Ràng buộc nghiệp vụ ngặt nghèo: Tuyệt đối chặn hành vi xóa nếu kho này ĐANG CÓ SỐ DƯ TỒN KHO > 0 
        /// hoặc đang có Phiếu nhập/xuất/điều chuyển ở trạng thái CHƯA HOÀN TẤT. 
        /// Nếu vi phạm, Service sẽ ném ra Exception chặn thao tác để bảo vệ an toàn kế toán.
        /// </summary>
        /// <param name="id">Mã định danh của kho cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm đóng của Kho hàng.
        /// Nghiệp vụ: Kho bị tạm đóng sẽ tự động bị loại khỏi thuật toán chia đơn (Smart Routing) 
        /// và không thể lập Phiếu nhập hàng mới.
        /// </summary>
        /// <param name="id">Mã định danh của kho cần thao tác.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}