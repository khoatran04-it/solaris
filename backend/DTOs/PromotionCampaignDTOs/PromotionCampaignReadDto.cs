using System;
using System.Collections.Generic;

namespace backend.DTOs.PromotionCampaignDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết thông tin Chiến Dịch Khuyến Mãi (Promotion Campaign).
    /// Trả về đầy đủ thông tin cấu hình giảm giá và danh sách chi tiết các mặt hàng (SKU) đang được áp dụng chiến dịch này.
    /// </summary>
    public class PromotionCampaignReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Nội dung
        /// <summary>Tên chiến dịch khuyến mãi.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Mô tả chi tiết và thể lệ của chương trình khuyến mãi.</summary>
        public string? Description { get; set; }
        #endregion

        #region Cấu hình Giảm giá (Discount Logic)
        /// <summary>Cờ xác định loại giảm giá (true: Giảm theo %, false: Giảm tiền mặt).</summary>
        public bool IsPercentage { get; set; }

        /// <summary>Giá trị giảm tương ứng.</summary>
        public decimal DiscountValue { get; set; }
        #endregion

        #region Thời gian Hiệu lực
        /// <summary>Thời điểm bắt đầu áp dụng khuyến mãi.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Thời điểm kết thúc chiến dịch.</summary>
        public DateTime EndDate { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang kích hoạt, false: Tạm ngưng).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Dữ liệu Liên kết (Enriched Data)
        /// <summary>
        /// Danh sách chi tiết các biến thể sản phẩm (SKU) đang được áp dụng khuyến mãi.
        /// Chứa đầy đủ thông tin Tên, Mã, Hình ảnh và Giá gốc để Frontend (Web/App) dễ dàng hiển thị danh sách sản phẩm Sale.
        /// </summary>
        public List<CampaignAppliedVariantDto> AppliedVariants { get; set; } = new List<CampaignAppliedVariantDto>();
        #endregion
    }

    /// <summary>
    /// DTO phụ trợ (Helper DTO) dùng để chứa thông tin chi tiết của một Biến thể sản phẩm (Variant) đang tham gia Chiến dịch Khuyến mãi.
    /// </summary>
    public class CampaignAppliedVariantDto
    {
        #region Thông tin Định danh
        /// <summary>Mã định danh (ID) của biến thể sản phẩm.</summary>
        public int VariantId { get; set; }

        /// <summary>Mã SKU của biến thể (Ví dụ: SKU-IP15-PRO-256-BLK).</summary>
        public string VariantCode { get; set; } = string.Empty;
        #endregion

        #region Thông tin Hiển thị
        /// <summary>Tên hiển thị đầy đủ của biến thể (Ví dụ: iPhone 15 Pro Max 256GB Đen).</summary>
        public string VariantName { get; set; } = string.Empty;

        /// <summary>Tên sản phẩm gốc (Parent Product).</summary>
        public string? ProductName { get; set; }

        /// <summary>Đường dẫn hình ảnh đại diện của biến thể.</summary>
        public string? ImagePath { get; set; }
        #endregion

        #region Giá & Đơn vị tính
        /// <summary>Giá bán lẻ mặc định chưa qua giảm giá (Dùng để hiển thị giá gạch chéo trên giao diện).</summary>
        public decimal DefaultPrice { get; set; }

        /// <summary>Tên Đơn vị tính mặc định (Ví dụ: Cái, Hộp, Chiếc).</summary>
        public string? DefaultUoMName { get; set; }
        #endregion
    }
}