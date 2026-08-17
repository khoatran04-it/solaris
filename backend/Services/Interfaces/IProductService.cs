using backend.DTOs;
using backend.DTOs.ProductDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Sản Phẩm Gốc (Product Master).
    /// </summary>
    public interface IProductService
    {
        /// <summary>Lấy toàn bộ danh sách sản phẩm không phân trang (hỗ trợ lọc chỉ sản phẩm đang hoạt động)</summary>
        Task<IEnumerable<ProductReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>Lấy danh sách sản phẩm có phân trang, tìm kiếm và bộ lọc đa luồng</summary>
        Task<PagedResult<ProductReadDto>> GetPagedAsync(
            string? search,
            string? categoryId,
            string? baseUoMId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>Lấy thông tin chi tiết sản phẩm theo ID</summary>
        Task<ProductReadDto> GetByIdAsync(int id);

        /// <summary>Tạo mới sản phẩm gốc</summary>
        Task<int> CreateAsync(ProductCreateDto dto);

        /// <summary>Cập nhật thông tin sản phẩm gốc</summary>
        Task<bool> UpdateAsync(int id, ProductUpdateDto dto);

        /// <summary>Xóa mềm sản phẩm (có kiểm tra ràng buộc Biến thể SKU, NCC, Quy đổi...)</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Thay đổi trạng thái Hoạt động (Đang bán / Ngừng bán)</summary>
        Task<bool> ToggleActiveAsync(int id);

        /// <summary>Lấy cấu hình thuộc tính động (EAV) của sản phẩm theo Danh mục</summary>
        Task<IEnumerable<dynamic>> GetDynamicAttributesConfigAsync(int productId);
    }
}