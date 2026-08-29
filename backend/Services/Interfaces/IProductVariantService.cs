using backend.DTOs;
using backend.DTOs.ProductVariantDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Biến Thể Sản Phẩm (SKU - Stock Keeping Unit).
    /// Chịu trách nhiệm xử lý nghiệp vụ cho các đơn vị hàng hóa vật lý cụ thể, bao gồm việc thiết lập Giá bán theo ĐVT và các Thuộc tính động (EAV).
    /// </summary>
    public interface IProductVariantService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách biến thể sản phẩm (không phân trang, thường dùng để nạp dữ liệu cho giao diện Dropdown/Select khi nhập kho).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các SKU đang ở trạng thái hoạt động (mặc định là false).</param>
        Task<IEnumerable<ProductVariantReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách biến thể sản phẩm có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã SKU hoặc tên biến thể.</param>
        /// <param name="productId">Chuỗi danh sách ID Sản phẩm cha (có thể phân cách bằng dấu phẩy nếu lọc theo nhiều sản phẩm gốc).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true: Đang kinh doanh, false: Tạm khóa).</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật thông tin cuối cùng.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi hiển thị trên mỗi trang.</param>
        Task<PagedResult<ProductVariantReadDto>> GetPagedAsync(
            string? search,
            string? productId,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một biến thể (SKU) theo ID. 
        /// Dữ liệu trả về bao gồm cả thông tin cơ bản, cấu hình Bảng giá theo ĐVT và các Giá trị thuộc tính (EAV).
        /// </summary>
        /// <param name="id">Mã định danh của biến thể.</param>
        Task<ProductVariantReadDto> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới một biến thể sản phẩm (SKU).
        /// Xử lý lưu đồng thời thông tin SKU, danh sách Giá bán theo các ĐVT và danh sách Thuộc tính động trong cùng một Transaction.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới biến thể.</param>
        /// <returns>ID của biến thể vừa được tạo.</returns>
        Task<int> CreateAsync(ProductVariantCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin biến thể sản phẩm.
        /// Bao gồm logic xóa cũ - thêm mới (hoặc cập nhật) đối với danh sách Giá bán và danh sách Thuộc tính động để đảm bảo dữ liệu nhất quán.
        /// </summary>
        /// <param name="id">Mã định danh của biến thể cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, ProductVariantUpdateDto dto);

        /// <summary>
        /// Xóa mềm một biến thể (SKU). 
        /// Hệ thống sẽ kiểm tra xem SKU này có đang tồn kho hoặc nằm trong các đơn hàng/lô hàng đang xử lý hay không trước khi cho phép xóa.
        /// </summary>
        /// <param name="id">Mã định danh của biến thể cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (Đang kinh doanh / Tạm khóa) của biến thể.
        /// </summary>
        /// <param name="id">Mã định danh của biến thể cần đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion
    }
}