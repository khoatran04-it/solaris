using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý và tính toán Quy tắc Quy đổi Đơn vị tính (UoM Conversion).
    /// </summary>
    public interface IUoMConversionService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách quy tắc quy đổi (không phân trang).
        /// </summary>
        /// <param name="isActiveOnly">Lọc chỉ lấy các quy tắc đang hoạt động (mặc định là false).</param>
        Task<IEnumerable<UoMConversionReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách quy tắc quy đổi có hỗ trợ tìm kiếm, lọc nâng cao, thời gian và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã/Tên ĐVT nguồn, đích hoặc tên sản phẩm).</param>
        /// <param name="productId">Lọc theo mã định danh sản phẩm.</param>
        /// <param name="isStandard">Lọc theo loại quy đổi (true: quy chuẩn hệ thống, false: đặc thù theo sản phẩm).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang.</param>
        Task<PagedResult<UoMConversionReadDto>> GetPagedAsync(
            string? search,
            int? productId,
            bool? isStandard,
            bool? isActive,
            DateTime? createdAt,
            DateTime? updatedAt,
            int pageIndex,
            int pageSize);

        /// <summary>
        /// Lấy thông tin chi tiết của một quy tắc quy đổi theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc quy đổi.</param>
        Task<UoMConversionReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command)
        /// <summary>
        /// Tạo mới quy tắc quy đổi (có thẩm định kiểm tra vòng lặp vô hạn và tính hợp lệ cùng nhóm ĐVT).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới quy tắc quy đổi.</param>
        /// <returns>ID của quy tắc vừa được tạo.</returns>
        Task<int> CreateAsync(UoMConversionCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin quy tắc quy đổi.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        Task<bool> UpdateAsync(int id, UoMConversionUpdateDto dto);

        /// <summary>
        /// Xóa mềm quy tắc quy đổi khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc cần xóa.</param>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động của quy tắc quy đổi.
        /// </summary>
        /// <param name="id">Mã định danh của quy tắc cần thay đổi trạng thái.</param>
        Task<bool> ToggleActiveAsync(int id);
        #endregion

        #region Thuật Toán & Quy Đổi Tồn Kho (Calculation Engine)
        /// <summary>
        /// Tìm hệ số quy đổi giữa 2 ĐVT bất kỳ cho một Biến thể sản phẩm (SKU).
        /// Áp dụng thuật toán ưu tiên: ĐVT cơ sở -> Quy đổi đặc thù sản phẩm -> Quy đổi tiêu chuẩn hệ thống -> Thuận/Nghịch.
        /// </summary>
        Task<decimal> GetConversionFactorAsync(int variantId, int fromUoMId, int toUoMId);

        /// <summary>
        /// Quy đổi một số lượng bất kỳ theo ĐVT chỉ định về Đơn vị tính cơ sở (Base UoM) của sản phẩm.
        /// Nghiệp vụ cốt lõi: Dùng làm chốt chặn trước khi cộng/trừ vào WarehouseInventory và InventoryTransaction.
        /// </summary>
        Task<decimal> ConvertToBaseQuantityAsync(int variantId, int fromUoMId, decimal quantity);

        /// <summary>
        /// Quy đổi từ số lượng theo ĐVT cơ sở (Base UoM) sang một ĐVT đích.
        /// </summary>
        Task<decimal> ConvertFromBaseQuantityAsync(int variantId, int targetUoMId, decimal baseQuantity);

        /// <summary>
        /// Trích xuất danh sách tất cả các Đơn vị tính hợp lệ của một Biến thể sản phẩm (0% hardcode):
        /// Bao gồm: Base UoM, các ĐVT trong bảng giá bán (Prices), ĐVT mua hàng (SupplierProducts), và bảng quy đổi (UoMConversions).
        /// </summary>
        Task<List<ValidUoMOptionDto>> GetValidUoMsForVariantAsync(int variantId);
        #endregion
    }
}