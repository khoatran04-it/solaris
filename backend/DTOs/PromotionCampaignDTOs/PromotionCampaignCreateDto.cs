using System;
using System.Collections.Generic;

namespace backend.DTOs.PromotionCampaignDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Chiến Dịch Khuyến Mãi (Promotion Campaign).
    /// Hỗ trợ thiết lập thông tin chiến dịch, mức giảm giá, thời gian hiệu lực và danh sách các sản phẩm (SKU) áp dụng.
    /// </summary>
    public class PromotionCampaignCreateDto
    {
        #region Thông tin Định danh & Nội dung
        /// <summary>Tên chiến dịch khuyến mãi (Ví dụ: Flash Sale Cuối Tuần, Mùa Thu Hoạch).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết và thể lệ của chương trình khuyến mãi.</summary>
        public string? Description { get; set; }
        #endregion

        #region Cấu hình Giảm giá (Discount Logic)
        /// <summary>Cờ xác định loại giảm giá (true: Giảm theo phần trăm %, false: Giảm trừ trực tiếp tiền mặt VNĐ).</summary>
        public bool IsPercentage { get; set; }

        /// <summary>Giá trị giảm (Ví dụ: Nhập 20 nếu giảm 20%, hoặc 50000 nếu giảm 50.000đ).</summary>
        public decimal DiscountValue { get; set; }
        #endregion

        #region Thời gian Hiệu lực
        /// <summary>Thời điểm bắt đầu áp dụng khuyến mãi.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Thời điểm kết thúc chiến dịch.</summary>
        public DateTime EndDate { get; set; }
        #endregion

        #region Trạng thái
        /// <summary>Trạng thái hoạt động (true: Kích hoạt chờ chạy theo lịch, false: Tạm dừng / Lưu nháp).</summary>
        public bool IsActive { get; set; } = true;
        #endregion

        #region Dữ liệu Liên kết (Khởi tạo đồng thời)
        /// <summary>Danh sách ID các biến thể sản phẩm (Variant) được áp dụng mức giảm giá trong chiến dịch này.</summary>
        public List<int> VariantIds { get; set; } = new List<int>();
        #endregion
    }
}