namespace backend.Models
{
    /// <summary>
    /// Thực thể Chiến Dịch Khuyến Mãi (Promotion Campaign).
    /// Quản lý các chương trình Flash Sale, xả kho, hoặc sự kiện giảm giá theo phần trăm / tiền mặt.
    /// Thiết lập khoảng thời gian hiệu lực và cho phép áp dụng trên nhiều biến thể sản phẩm (SKU) khác nhau.
    /// </summary>
    public class PromotionCampaign : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh & Nội dung
        /// <summary>Tên chiến dịch khuyến mãi (Ví dụ: Flash Sale Cuối Tuần, Giảm Giá Mùa Thu Hoạch).</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn thân thiện cho SEO để hiển thị trên URL (Ví dụ: flash-sale-cuoi-tuan).</summary>
        public string? Slug { get; set; }

        /// <summary>Đường dẫn ảnh Banner quảng cáo, dùng để hiển thị trên trang chủ hoặc trang landing page của khuyến mãi.</summary>
        public string? BannerImagePath { get; set; }

        /// <summary>Mô tả chi tiết và thể lệ của chương trình khuyến mãi (Có thể lưu trữ định dạng Rich Text/HTML).</summary>
        public string? Description { get; set; }
        #endregion

        #region Cấu hình Giảm giá (Discount Logic)
        /// <summary>Cờ xác định loại giảm giá (true: Giảm theo % giá bán, false: Giảm trừ trực tiếp tiền mặt VNĐ).</summary>
        public bool IsPercentage { get; set; }

        /// <summary>Giá trị giảm (Ví dụ: Nhập 20 nếu IsPercentage = true tức là giảm 20%. Hoặc nhập 50000 nếu IsPercentage = false tức là giảm thẳng 50.000đ).</summary>
        public decimal DiscountValue { get; set; }
        #endregion

        #region Thời gian Hiệu lực
        /// <summary>Thời điểm bắt đầu áp dụng khuyến mãi (Hệ thống sẽ tự động kích hoạt giá khuyến mãi khi đến giờ).</summary>
        public DateTime StartDate { get; set; }

        /// <summary>Thời điểm kết thúc chiến dịch (Hệ thống sẽ tự động đưa giá sản phẩm về giá gốc khi quá hạn).</summary>
        public DateTime EndDate { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Cờ đánh dấu kích hoạt thủ công (true: Cho phép chạy theo thời gian Start/End, false: Dừng khẩn cấp / Tạm ngưng chiến dịch ngay lập tức).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Danh sách các biến thể sản phẩm (SKU) cụ thể được chọn để áp dụng mức giảm giá của chiến dịch này.</summary>
        public virtual ICollection<PromotionVariant> PromotionVariants { get; set; } = new List<PromotionVariant>();
        #endregion
    }
}