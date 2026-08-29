using backend.DTOs;
using backend.DTOs.ProductDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Sản Phẩm Cha (Product Master).
    /// Chịu trách nhiệm xử lý nghiệp vụ cho dòng sản phẩm chung trước khi phân rã thành các Biến thể (SKU) chi tiết.
    /// </summary>
    public interface IProductService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách sản phẩm (không phân trang, thường dùng để nạp dữ liệu cho giao diện Dropdown/Select).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các sản phẩm đang ở trạng thái hoạt động (mặc định là false).</param>
        Task<IEnumerable<ProductReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách sản phẩm có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu đa chiều.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã (Code) hoặc tên (Name) sản phẩm.</param>
        /// <param name="categoryId">Chuỗi danh sách ID Danh mục sản phẩm (có thể phân cách bằng dấu phẩy nếu lọc nhiều).</param>
        /// <param name="baseUoMId">Chuỗi danh sách ID Đơn vị tính cơ sở (có thể phân cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true: Đang kinh doanh, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
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

        /// <summary>
        /// Lấy thông tin chi tiết của một sản phẩm cha theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của sản phẩm.</param>
        Task<ProductReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Khởi tạo mới một dòng sản phẩm (Product Master) vào hệ thống.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới sản phẩm.</param>
        /// <returns>ID của sản phẩm vừa được tạo.</returns>
        Task<int> CreateAsync(ProductCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin chung của sản phẩm cha.
        /// </summary>
        /// <param name="id">Mã định danh của sản phẩm cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, ProductUpdateDto dto);

        /// <summary>
        /// Xóa mềm một sản phẩm. 
        /// Hệ thống sẽ tự động kiểm tra xem sản phẩm này có đang chứa các Biến thể (SKU) đã phát sinh giao dịch (Tồn kho, Lô hàng...) hay không trước khi xóa.
        /// </summary>
        /// <param name="id">Mã định danh của sản phẩm cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang bán / Ngừng bán) của sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của sản phẩm cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion

        #region Mở rộng (Extension & EAV)
        /// <summary>
        /// Lấy cấu hình các thuộc tính động (EAV Template) mà một sản phẩm cần phải nhập dữ liệu, 
        /// dựa trên cấu hình Danh mục (Category) mà sản phẩm đó trực thuộc.
        /// Hỗ trợ UI sinh ra các Form nhập liệu linh hoạt cho từng loại nông sản.
        /// </summary>
        /// <param name="productId">Mã định danh của sản phẩm cần lấy cấu hình thuộc tính.</param>
        /// <returns>Danh sách các đối tượng cấu hình thuộc tính động (dynamic schema).</returns>
        Task<IEnumerable<dynamic>> GetDynamicAttributesConfigAsync(int productId);
        #endregion
    }
}