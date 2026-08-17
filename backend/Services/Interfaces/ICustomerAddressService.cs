using backend.DTOs.CustomerAddressDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Sổ địa chỉ giao hàng của khách hàng.
    /// </summary>
    public interface ICustomerAddressService
    {
        /// <summary>Lấy danh sách địa chỉ của một khách hàng</summary>
        Task<IEnumerable<CustomerAddressReadDto>> GetByCustomerIdAsync(int customerId);

        /// <summary>Lấy chi tiết địa chỉ theo ID</summary>
        Task<CustomerAddressReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới địa chỉ giao hàng cho khách hàng</summary>
        Task<int> CreateAsync(int customerId, CustomerAddressCreateDto dto);

        /// <summary>Cập nhật thông tin địa chỉ</summary>
        Task<bool> UpdateAsync(int id, CustomerAddressUpdateDto dto);

        /// <summary>Xóa địa chỉ (có kiểm tra đơn hàng đang sử dụng)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chỉ định một địa chỉ làm mặc định</summary>
        Task<bool> SetDefaultAsync(int id, int customerId);
    }
}