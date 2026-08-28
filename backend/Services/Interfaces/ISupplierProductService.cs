using backend.DTOs;
using backend.DTOs.SupplierProductDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Bảng giá và Danh mục mặt hàng từ Nhà cung cấp (Supplier Product).
    /// </summary>
    public interface ISupplierProductService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách mặt hàng của nhà cung cấp (không phân trang).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các mặt hàng đang hoạt động (mặc định là false).</param>
        Task<IEnumerable<SupplierProductReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh mục các mặt hàng được cung cấp bởi một Nhà cung cấp cụ thể.
        /// </summary>
        /// <param name="supplierId">Mã định danh của Nhà cung cấp.</param>
        /// <param name="isActiveOnly">Lọc chỉ lấy các mặt hàng đang hoạt động (mặc định là true).</param>
        Task<IEnumerable<SupplierProductReadDto>> GetBySupplierIdAsync(int supplierId, bool isActiveOnly = true);

        /// <summary>
        /// Lấy danh sách mặt hàng của nhà cung cấp có hỗ trợ tìm kiếm, lọc nâng cao, thời gian và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã SKU, tên sản phẩm hoặc tên nhà cung cấp.</param>
        /// <param name="variantId">Lọc theo mã định danh Biến thể sản phẩm (SKU hệ thống).</param>
        /// <param name="supplierId">Lọc theo mã định danh Nhà cung cấp.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật cuối.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<SupplierProductReadDto>> GetPagedAsync(
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
        /// Lấy thông tin chi tiết của một mặt hàng thuộc nhà cung cấp theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi mặt hàng - nhà cung cấp (SupplierProduct ID).</param>
        Task<SupplierProductReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Thiết lập mới thông tin mặt hàng và bảng giá cho nhà cung cấp.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới.</param>
        /// <returns>ID của bản ghi vừa được tạo.</returns>
        Task<int> CreateAsync(SupplierProductCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin giá nhập, MOQ, hoặc Lead Time của mặt hàng từ nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, SupplierProductUpdateDto dto);

        /// <summary>
        /// Xóa mềm một mặt hàng khỏi danh mục của nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (cung cấp / ngừng cung cấp) của mặt hàng này.
        /// </summary>
        /// <param name="id">Mã định danh của bản ghi.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}