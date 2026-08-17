using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public interface IProductCategoryService
    {
        /// <summary>Lấy toàn bộ danh sách danh mục sản phẩm (hỗ trợ lọc chỉ danh mục đang hoạt động)</summary>
        Task<IEnumerable<ProductCategoryReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách danh mục có phân trang và bộ lọc</summary>
        Task<PagedResult<ProductCategoryReadDto>> GetPagedAsync(
            string? search,
            string? names,
            string? categoryGroupId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>Lấy chi tiết danh mục theo ID</summary>
        Task<ProductCategoryReadDto?> GetByIdAsync(int id);

        /// <summary>Tạo mới danh mục sản phẩm</summary>
        Task<int> CreateAsync(ProductCategoryCreateDto dto);

        /// <summary>Cập nhật thông tin danh mục</summary>
        Task<bool> UpdateAsync(int id, ProductCategoryUpdateDto dto);

        /// <summary>Xóa danh mục (có kiểm tra ràng buộc sản phẩm trực thuộc)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Chuyển đổi trạng thái Hoạt động / Khóa của danh mục</summary>
        Task<bool> ToggleActiveAsync(int id);
    }
}
