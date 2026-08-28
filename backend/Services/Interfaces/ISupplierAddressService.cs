using backend.DTOs.SupplierAddressDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Sổ địa chỉ kho / giao nhận của Nhà cung cấp.
    /// </summary>
    public interface ISupplierAddressService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách địa chỉ của một nhà cung cấp (địa chỉ mặc định sẽ tự động được xếp lên đầu).
        /// </summary>
        /// <param name="supplierId">Mã định danh của Nhà cung cấp.</param>
        Task<IEnumerable<SupplierAddressReadDto>> GetBySupplierIdAsync(int supplierId);

        /// <summary>
        /// Lấy thông tin chi tiết của một địa chỉ theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ.</param>
        Task<SupplierAddressReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Thêm mới một địa chỉ giao nhận cho nhà cung cấp.
        /// </summary>
        /// <param name="supplierId">Mã định danh của nhà cung cấp sở hữu địa chỉ này.</param>
        /// <param name="dto">Dữ liệu tạo mới địa chỉ.</param>
        /// <returns>ID của địa chỉ vừa được tạo.</returns>
        Task<int> CreateAsync(int supplierId, SupplierAddressCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin của một địa chỉ giao nhận.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, SupplierAddressUpdateDto dto);

        /// <summary>
        /// Xóa mềm một địa chỉ (nếu xóa trúng địa chỉ mặc định, hệ thống sẽ tự động luân chuyển quyền mặc định sang địa chỉ khác).
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Thiết lập một địa chỉ làm địa chỉ giao nhận / lấy hàng mặc định cho nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của địa chỉ được chọn làm mặc định.</param>
        /// <param name="supplierId">Mã định danh của nhà cung cấp để đối chiếu hợp lệ.</param>
        Task<bool> SetDefaultAsync(int id, int supplierId);
        #endregion
    }
}