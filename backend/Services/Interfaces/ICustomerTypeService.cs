using backend.DTOs;
using backend.DTOs.CustomerTypeDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Phân loại khách hàng (Customer Type).
    /// </summary>
    public interface ICustomerTypeService
    {
        /// <summary>Lấy toàn bộ danh sách phân loại (dùng cho dropdown/combobox)</summary>
        Task<IEnumerable<CustomerTypeReadDto>> GetAllListAsync();

        /// <summary>Lấy danh sách phân loại có phân trang và bộ lọc</summary>
        Task<PagedResult<CustomerTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết phân loại theo ID</summary>
        Task<CustomerTypeReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới phân loại khách hàng</summary>
        Task<int> CreateAsync(CustomerTypeCreateDto dto);

        /// <summary>Cập nhật thông tin phân loại</summary>
        Task<bool> UpdateAsync(int id, CustomerTypeUpdateDto dto);

        /// <summary>Xóa phân loại (có kiểm tra khách hàng liên kết)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Tạm khóa</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}