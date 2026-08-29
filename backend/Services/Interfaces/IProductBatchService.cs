using backend.DTOs;
using backend.DTOs.ProductBatchDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Lô Hàng Nông Sản (Product Batches / Lots).
    /// Quản lý thông tin ngày sản xuất, hạn sử dụng, nhà cung cấp và biến thể sản phẩm theo từng lô nhập.
    /// </summary>
    public interface IProductBatchService
    {
        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách lô hàng không phân trang.
        /// </summary>
        Task<IEnumerable<ProductBatchReadDto>> GetAllListAsync();

        /// <summary>
        /// Lấy danh sách lô hàng có hỗ trợ phân trang, tìm kiếm và bộ lọc đa tiêu chí.
        /// </summary>
        /// <param name="search">Tìm kiếm theo mã lô hàng (BatchCode).</param>
        /// <param name="variantId">Lọc lô hàng theo ID biến thể sản phẩm.</param>
        /// <param name="supplierId">Lọc lô hàng theo ID nhà cung cấp.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật.</param>
        /// <param name="pageIndex">Chỉ số trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<ProductBatchReadDto>> GetPagedAsync(
            string? search,
            int? variantId,
            int? supplierId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết một lô hàng theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của lô hàng.</param>
        Task<ProductBatchReadDto> GetByIdAsync(int id);

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một lô hàng vào hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới lô hàng.</param>
        /// <returns>ID của lô hàng vừa được tạo.</returns>
        Task<int> CreateAsync(ProductBatchCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin lô hàng.
        /// </summary>
        /// <param name="id">Mã định danh của lô hàng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, ProductBatchUpdateDto dto);

        /// <summary>
        /// Xóa một lô hàng khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh của lô hàng cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Thay đổi trạng thái hoạt động của lô hàng.
        /// </summary>
        /// <param name="id">Mã định danh của lô hàng cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);

        #endregion
    }
}