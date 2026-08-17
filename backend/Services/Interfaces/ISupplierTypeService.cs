using backend.DTOs;
using backend.DTOs.SupplierTypeDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Quản lý Phân loại Nhà cung cấp (Supplier Type).
    /// </summary>
    public interface ISupplierTypeService
    {
        /// <summary>
        /// Lấy toàn bộ danh sách phân loại (không phân trang, phục vụ Dropdown / Select).
        /// </summary>
        Task<IEnumerable<SupplierTypeReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách phân loại có phân trang, hỗ trợ tìm kiếm và lọc trạng thái.
        /// </summary>
        Task<PagedResult<SupplierTypeReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy chi tiết phân loại theo ID.
        /// </summary>
        Task<SupplierTypeReadDto?> GetByIdAsync(int id);

        /// <summary>
        /// Tạo mới phân loại nhà cung cấp.
        /// </summary>
        Task<int> CreateAsync(SupplierTypeCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin phân loại nhà cung cấp.
        /// </summary>
        Task<bool> UpdateAsync(int id, SupplierTypeUpdateDto dto);

        /// <summary>
        /// Xóa mềm phân loại nhà cung cấp (kiểm tra ràng buộc dữ liệu).
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Chuyển đổi trạng thái hoạt động (Hoạt động / Tạm khóa).
        /// </summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}