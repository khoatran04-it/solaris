using backend.DTOs;
using backend.DTOs.PromotionCampaignDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service quản lý Chiến Dịch Khuyến Mãi (Promotion Campaign).
    /// Chịu trách nhiệm thiết lập chương trình giảm giá, khung thời gian hiệu lực và điều phối danh sách các sản phẩm (SKU) áp dụng sale.
    /// </summary>
    public interface IPromotionCampaignService
    {
        #region Truy vấn (Query)
        /// <summary>
        /// Lấy toàn bộ danh sách Chiến dịch (không phân trang).
        /// Thường dùng cho các bộ lọc hoặc Dropdown list trên giao diện Quản trị.
        /// </summary>
        /// <param name="isActiveOnly">Nếu true, chỉ lấy các chiến dịch đang Kích hoạt. Nếu false, lấy tất cả.</param>
        Task<IEnumerable<PromotionCampaignReadDto>> GetAllListAsync(bool isActiveOnly = false);

        /// <summary>
        /// Lấy danh sách Chiến dịch có hỗ trợ phân trang và bộ lọc nâng cao.
        /// Thường dùng để hiển thị lên DataGrid trang danh sách Khuyến mãi.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (theo tên chiến dịch).</param>
        /// <param name="isActive">Lọc theo trạng thái (true: Đang hoạt động, false: Tạm dừng).</param>
        /// <param name="startDate">Lọc các chiến dịch bắt đầu từ khoảng thời gian này.</param>
        /// <param name="endDate">Lọc các chiến dịch kết thúc trong khoảng thời gian này.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        Task<PagedResult<PromotionCampaignReadDto>> GetPagedAsync(
            string? search,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int pageIndex,
            int pageSize
        );

        /// <summary>
        /// Lấy thông tin chi tiết của một Chiến dịch theo ID.
        /// Nghiệp vụ: Trả về bao gồm cả thông tin cấu hình vỏ chiến dịch LẪN danh sách chi tiết các Biến thể (Variants) đang được áp dụng mức giảm giá này.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch.</param>
        /// <returns>Thông tin DTO của chiến dịch, hoặc null nếu không tồn tại.</returns>
        Task<PromotionCampaignReadDto?> GetByIdAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command) - Cấu hình vỏ Chiến dịch
        /// <summary>
        /// Tạo mới một vỏ Chiến dịch khuyến mãi (Tên, %, Thời gian).
        /// Nghiệp vụ: DTO tạo mới cũng có thể đính kèm mảng VariantIds để khởi tạo luôn danh sách sản phẩm ở bước này.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới.</param>
        /// <returns>ID của chiến dịch vừa được tạo.</returns>
        Task<int> CreateAsync(PromotionCampaignCreateDto dto);

        /// <summary>
        /// Cập nhật thông tin cấu hình vỏ Chiến dịch (Đổi tên, gia hạn ngày kết thúc, đổi mức % giảm).
        /// Lưu ý: Hàm này KHÔNG can thiệp vào danh sách sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy.</returns>
        Task<bool> UpdateAsync(int id, PromotionCampaignUpdateDto dto);

        /// <summary>
        /// Xóa (mềm) một Chiến dịch khuyến mãi.
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch cần xóa.</param>
        /// <returns>True nếu xóa mềm thành công.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Bật / Tắt trạng thái kích hoạt của Chiến dịch (Dùng khi muốn dừng khẩn cấp một chương trình Flash Sale).
        /// </summary>
        /// <param name="id">Mã định danh của chiến dịch.</param>
        /// <returns>True nếu thay đổi trạng thái thành công.</returns>
        Task<bool> ToggleActiveAsync(int id);
        #endregion

        #region Thao tác Dữ liệu (Command) - Áp dụng Sản phẩm (Variants)
        /// <summary>
        /// Bổ sung (Add) một danh sách các biến thể sản phẩm vào chiến dịch.
        /// Nghiệp vụ: Thường được gọi khi Admin mở popup và chọn thêm hàng loạt sản phẩm tham gia chương trình Sale.
        /// </summary>
        /// <param name="campaignId">Mã định danh của chiến dịch.</param>
        /// <param name="dto">Danh sách ID các biến thể (SKU) cần thêm vào.</param>
        /// <returns>True nếu quá trình thêm mới thành công.</returns>
        Task<bool> AddVariantsToCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto);

        /// <summary>
        /// Gỡ bỏ (Remove) một danh sách các biến thể sản phẩm khỏi chiến dịch.
        /// Nghiệp vụ: Thường được gọi khi Admin muốn hủy áp dụng khuyến mãi đối với một số mặt hàng cụ thể.
        /// </summary>
        /// <param name="campaignId">Mã định danh của chiến dịch.</param>
        /// <param name="dto">Danh sách ID các biến thể (SKU) cần gỡ bỏ.</param>
        /// <returns>True nếu quá trình gỡ bỏ thành công.</returns>
        Task<bool> RemoveVariantsFromCampaignAsync(int campaignId, ApplyVariantsToCampaignDto dto);
        #endregion
    }
}