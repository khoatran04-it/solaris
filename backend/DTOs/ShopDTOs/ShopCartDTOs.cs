using System.Collections.Generic;

namespace backend.DTOs.ShopDTOs
{
    #region 1. DTO Hiển Thị Giỏ Hàng (Read DTOs)
    /// <summary>
    /// DTO Hiển thị chi tiết một dòng sản phẩm trong Giỏ hàng của Khách hàng.
    /// Đã được làm phẳng (Flatten) các thông tin về Giá, Chương trình khuyến mãi, Hình ảnh và Tình trạng Tồn kho thực tế.
    /// </summary>
    public class ShopCartItemDto
    {
        public int Id { get; set; }

        #region Hàng hóa & Định danh (Product & Variant)
        public int VariantId { get; set; }
        public required string VariantName { get; set; }
        public required string VariantCode { get; set; }

        /// <summary>Slug của sản phẩm dùng để tạo đường dẫn thân thiện trên SEO Web (Ví dụ: /san-pham/ca-chua-cherry).</summary>
        public string? ProductSlug { get; set; }

        /// <summary>Đường dẫn hình ảnh thu nhỏ (Thumbnail) hiển thị trên giao diện giỏ hàng.</summary>
        public string? ImagePath { get; set; }
        #endregion

        #region Đơn vị tính (UoM)
        public int UoMId { get; set; }
        public required string UoMName { get; set; } // Ví dụ: Kg, Hộp, Thùng
        #endregion

        #region Khối lượng & Tài chính (Pricing & Totals)
        /// <summary>Số lượng sản phẩm khách chọn mua trong giỏ.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Đơn giá hiện hành sau khi đã áp dụng khuyến mãi (VND).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Đơn giá gốc trước khi giảm giá (VND) - Dùng để gạch ngang hiển thị trên UI.</summary>
        public decimal OriginalPrice { get; set; }

        /// <summary>Số tiền được giảm trên mỗi đơn vị sản phẩm.</summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>Thành tiền của dòng sản phẩm này (Quantity * UnitPrice).</summary>
        public decimal TotalPrice { get; set; }
        #endregion

        #region Tình trạng Kho & Nguồn gốc (Inventory Check)
        /// <summary>Số lượng tồn kho khả dụng hiện tại (QuantityAvailable).</summary>
        public decimal AvailableStock { get; set; }

        /// <summary>
        /// Cờ báo hết hàng (True nếu khách chọn mua quá số lượng tồn kho khả dụng).
        /// Giúp Frontend tô đỏ dòng sản phẩm và khóa nút Thanh toán nếu cần.
        /// </summary>
        public bool IsOutOfStock { get; set; }

        /// <summary>Nguồn gốc / Xuất xứ nông sản (Ví dụ: Đà Lạt, Tiền Giang).</summary>
        public string? Origin { get; set; }

        /// <summary>Cờ đánh dấu sản phẩm thuộc nhóm chuỗi lạnh (thịt, cá, hải sản, rau củ tươi sống).</summary>
        public bool RequiresColdChain { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO Tổng quan Giỏ hàng (Bao gồm danh sách sản phẩm và các chỉ số tổng tiền để hiển thị lên thanh Checkout Sidebar).
    /// </summary>
    public class ShopCartDto
    {
        public int CartId { get; set; }

        /// <summary>Danh sách các mặt hàng đang có trong giỏ.</summary>
        public List<ShopCartItemDto> Items { get; set; } = new List<ShopCartItemDto>();

        /// <summary>Tổng số lượng các dòng sản phẩm trong giỏ (Badge hiển thị trên icon giỏ hàng).</summary>
        public int TotalItems { get; set; }

        /// <summary>Tổng tiền hàng tạm tính trước giảm giá.</summary>
        public decimal SubTotal { get; set; }

        /// <summary>Tổng số tiền được chiết khấu / giảm giá.</summary>
        public decimal TotalDiscount { get; set; }

        /// <summary>Tổng tiền ước tính thanh toán cuối cùng (Estimated Total).</summary>
        public decimal EstimatedTotal { get; set; }

        /// <summary>Cờ tổng hợp: Có ít nhất một sản phẩm trong giỏ thuộc chuỗi lạnh (thịt, cá, rau củ tươi sống).</summary>
        public bool HasColdChain { get; set; }
    }
    #endregion

    #region 2. DTO Thao tác Thay đổi Giỏ hàng (Command DTOs)
    /// <summary>
    /// DTO Thêm sản phẩm vào giỏ hàng từ trang danh sách hoặc trang chi tiết sản phẩm.
    /// </summary>
    public class ShopCartAddDto
    {
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// DTO Cập nhật số lượng của một dòng sản phẩm trong giỏ hàng (Khi khách bấm nút cộng/trừ tại trang Giỏ hàng).
    /// </summary>
    public class ShopCartUpdateDto
    {
        public decimal Quantity { get; set; }
    }
    #endregion

    #region 3. DTO Đồng bộ Giỏ hàng Khách Vãng Lai (Guest Cart Synchronization)
    /// <summary>
    /// Đại diện cho một mặt hàng nằm trong giỏ lưu tạm trên trình duyệt của Khách vãng lai (Local Storage / Cookie).
    /// </summary>
    public class ShopGuestCartItemDto
    {
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// DTO Yêu cầu Đồng bộ Giỏ hàng Vãng lai (Sync Guest Cart).
    /// NGHIỆP VỤ TRẢI NGHIỆM: Khi khách hàng chưa đăng nhập (Khách vãng lai) chọn vài món vào giỏ, 
    /// sau đó họ bấm nút Đăng nhập, Frontend sẽ gom toàn bộ giỏ hàng lưu tạm trên máy khách 
    /// gói gọn vào DTO này và gửi lên Server để gộp chung vào tài khoản Server-side Cart của họ.
    /// </summary>
    public class ShopSyncGuestCartDto
    {
        public List<ShopGuestCartItemDto> Items { get; set; } = new List<ShopGuestCartItemDto>();
    }
    #endregion
}