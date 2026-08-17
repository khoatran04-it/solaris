using backend.DTOs.SupplierAddressDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Sổ địa chỉ kho của Nhà cung cấp.
    /// </summary>
    public interface ISupplierAddressService
    {
        /// <summary>Lấy danh sách địa chỉ của một nhà cung cấp (địa chỉ mặc định lên đầu)</summary>
        Task<IEnumerable<SupplierAddressReadDto>> GetBySupplierIdAsync(int supplierId);

        /// <summary>Lấy chi tiết một địa chỉ theo ID</summary>
        Task<SupplierAddressReadDto?> GetByIdAsync(int id);

        /// <summary>Thêm địa chỉ mới cho nhà cung cấp</summary>
        Task<int> CreateAsync(int supplierId, SupplierAddressCreateDto dto);

        /// <summary>Cập nhật thông tin địa chỉ</summary>
        Task<bool> UpdateAsync(int id, SupplierAddressUpdateDto dto);

        /// <summary>Xóa địa chỉ (tự động luân chuyển quyền mặc định nếu xóa địa chỉ mặc định)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Đặt một địa chỉ làm địa chỉ lấy hàng mặc định</summary>
        Task<bool> SetDefaultAsync(int id, int supplierId);
    }
}