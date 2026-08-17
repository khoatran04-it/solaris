using backend.DTOs;
using backend.DTOs.CustomerGroupDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Nhóm Khách Hàng (Customer Group).
    /// </summary>
    public interface ICustomerGroupService
    {
        /// <summary>Lấy toàn bộ danh sách nhóm khách hàng (hỗ trợ lọc chỉ lấy nhóm đang hoạt động)</summary>
        Task<IEnumerable<CustomerGroupReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách nhóm khách hàng có phân trang và bộ lọc</summary>
        Task<PagedResult<CustomerGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết nhóm khách hàng theo ID</summary>
        Task<CustomerGroupReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới nhóm khách hàng</summary>
        Task<int> CreateAsync(CustomerGroupCreateDto dto);

        /// <summary>Cập nhật thông tin nhóm khách hàng</summary>
        Task<bool> UpdateAsync(int id, CustomerGroupUpdateDto dto);

        /// <summary>Xóa nhóm khách hàng (có kiểm tra ràng buộc liên kết thành viên)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Khóa của nhóm</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}