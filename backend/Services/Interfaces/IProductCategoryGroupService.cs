using backend.DTOs;
using backend.DTOs.ProductCategoryGroupDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Nhóm Ngành Hàng (Product Category Group).
    /// </summary>
    public interface IProductCategoryGroupService
    {
        /// <summary>Lấy toàn bộ danh sách nhóm ngành hàng (hỗ trợ lọc chỉ nhóm đang hoạt động)</summary>
        Task<IEnumerable<ProductCategoryGroupReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách nhóm ngành hàng có phân trang và bộ lọc</summary>
        Task<PagedResult<ProductCategoryGroupReadDto>> GetPagedAsync(
            string? search,
            string? names,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết nhóm ngành hàng theo ID</summary>
        Task<ProductCategoryGroupReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới nhóm ngành hàng</summary>
        Task<int> CreateAsync(ProductCategoryGroupCreateDto dto);

        /// <summary>Cập nhật thông tin nhóm ngành hàng</summary>
        Task<bool> UpdateAsync(int id, ProductCategoryGroupUpdateDto dto);

        /// <summary>Xóa nhóm ngành hàng (có kiểm tra ràng buộc danh mục con)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Khóa của nhóm ngành hàng</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}
