using backend.DTOs;
using backend.DTOs.CustomerDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Khách Hàng (Customer).
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>Lấy toàn bộ danh sách khách hàng (không phân trang, dùng cho dropdown/lookup)</summary>
        Task<IEnumerable<CustomerReadDto>> GetAllListAsync();

        /// <summary>Lấy danh sách khách hàng có phân trang và bộ lọc nâng cao</summary>
        Task<PagedResult<CustomerReadDto>> GetPagedAsync(
            string? search,
            string? customerTypeId,
            string? customerTierId,
            string? customerGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết hồ sơ khách hàng theo ID kèm sổ địa chỉ và nhóm</summary>
        Task<CustomerReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới hồ sơ khách hàng (kèm nhóm và địa chỉ mặc định đầu tiên nếu có)</summary>
        Task<int> CreateAsync(CustomerCreateDto dto);

        /// <summary>Cập nhật thông tin khách hàng</summary>
        Task<bool> UpdateAsync(int id, CustomerUpdateDto dto);

        /// <summary>Xóa khách hàng (có kiểm tra ràng buộc đơn hàng/phiếu trả hàng)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Tạm khóa của khách hàng</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}