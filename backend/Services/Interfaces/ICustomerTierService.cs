using backend.DTOs;
using backend.DTOs.CustomerTierDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Bậc hạng khách hàng (Customer Loyalty Tier).
    /// </summary>
    public interface ICustomerTierService
    {
        /// <summary>Lấy toàn bộ danh sách bậc hạng (dùng cho dropdown/combobox)</summary>
        Task<IEnumerable<CustomerTierReadDto>> GetAllListAsync();

        /// <summary>Lấy danh sách bậc hạng có phân trang và bộ lọc</summary>
        Task<PagedResult<CustomerTierReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết bậc hạng theo ID</summary>
        Task<CustomerTierReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới bậc hạng khách hàng</summary>
        Task<int> CreateAsync(CustomerTierCreateDto dto);

        /// <summary>Cập nhật thông tin bậc hạng</summary>
        Task<bool> UpdateAsync(int id, CustomerTierUpdateDto dto);

        /// <summary>Xóa bậc hạng (có kiểm tra ràng buộc khách hàng liên kết)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Tạm khóa</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}