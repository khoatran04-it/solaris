using System.Collections.Generic;

namespace backend.DTOs.PromotionCampaignDTOs
{
    /// <summary>
    /// DTO yêu cầu áp dụng (hoặc cập nhật) danh sách các sản phẩm vào một Chiến dịch khuyến mãi.
    /// Dùng cho một API riêng biệt, chuyên xử lý việc thêm/bớt sản phẩm khỏi chương trình Sale.
    /// </summary>
    public class ApplyVariantsToCampaignDto
    {
        #region Dữ liệu Liên kết
        /// <summary>
        /// Danh sách ID của các biến thể sản phẩm (SKU) sẽ được áp dụng mức giảm giá của chiến dịch này.
        /// (Gửi lên một mảng rỗng [] nếu muốn gỡ bỏ toàn bộ sản phẩm khỏi chiến dịch).
        /// </summary>
        public List<int> VariantIds { get; set; } = new List<int>();
        #endregion
    }
}