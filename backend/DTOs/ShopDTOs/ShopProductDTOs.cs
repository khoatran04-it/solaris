namespace backend.DTOs.ShopDTOs
{
    /// <summary>
    /// DTO hiển thị Thẻ Sản phẩm (Product Card).
    /// Dùng cho các trang danh sách, kết quả tìm kiếm, trang chủ (hiển thị dạng Grid/List).
    /// </summary>
    public class ShopProductCardDto
    {
        public int Id { get; set; }
        public int? VariantId { get; set; }
        public int? ProductId { get; set; }

        #region Định danh & SEO
        /// <summary>Mã SKU biến thể hoặc mã sản phẩm chung.</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị trên thẻ (Tên biến thể hoặc Dòng sản phẩm).</summary>
        public required string Name { get; set; }

        /// <summary>Tên dòng sản phẩm cha (Ví dụ: Thịt Heo Sạch C.P).</summary>
        public string? ProductName { get; set; }

        /// <summary>Tên biến thể cụ thể (Ví dụ: Đùi Heo 1Kg, Má Heo).</summary>
        public string? VariantName { get; set; }

        /// <summary>Đường dẫn thân thiện hỗ trợ SEO của dòng sản phẩm cha (Ví dụ: thit-heo-sach-cp).</summary>
        public required string Slug { get; set; }
        #endregion

        #region Chi tiết & Phân loại
        /// <summary>Ảnh đại diện (Thumbnail) của sản phẩm.</summary>
        public string? ImagePath { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }

        public string? CategoryGroupName { get; set; }
        public string? CategoryGroupSlug { get; set; }

        /// <summary>Đơn vị tính cơ sở (Ví dụ: Kg, Quả).</summary>
        public string BaseUoMName { get; set; } = string.Empty;
        #endregion

        #region Giá bán & Khuyến mãi (Theo biến thể mặc định)
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }

        /// <summary>Phần trăm giảm giá hiển thị trên Label đỏ (Ví dụ: 15).</summary>
        public decimal DiscountPercent { get; set; }

        public bool HasPromotion { get; set; }
        public string? PromotionName { get; set; }
        #endregion

        #region Thuộc tính EAV nổi bật (Tag Nông sản)
        /// <summary>Vùng trồng (Ví dụ: Đà Lạt, Lâm Đồng, New Zealand).</summary>
        public string? Origin { get; set; }

        /// <summary>Chứng nhận chất lượng (Ví dụ: VietGAP, GlobalGAP, Organic).</summary>
        public string? Certification { get; set; }

        /// <summary>Độ ngọt (Ví dụ: 12 Brix).</summary>
        public string? BrixLevel { get; set; }
        #endregion

        #region Trạng thái Kho
        /// <summary>Cờ báo còn hàng hay hết hàng (Hỗ trợ làm mờ thẻ sản phẩm nếu hết).</summary>
        public bool IsInStock { get; set; }

        /// <summary>Tổng tồn kho hiện có thể bán.</summary>
        public decimal TotalAvailableStock { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO hiển thị Trang Chi tiết Sản phẩm (Product Detail Page - PDP).
    /// </summary>
    public class ShopProductDetailDto
    {
        public int Id { get; set; }

        #region Định danh & Nội dung
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }

        /// <summary>Bài viết mô tả chi tiết sản phẩm (Render dưới dạng HTML).</summary>
        public string? Description { get; set; }

        /// <summary>Ảnh đại diện lớn.</summary>
        public string? ImagePath { get; set; }
        #endregion

        #region Phân loại & Đơn vị tính
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }
        public string? CategoryGroupName { get; set; }
        public string? CategoryGroupSlug { get; set; }

        public int BaseUoMId { get; set; }
        public string BaseUoMName { get; set; } = string.Empty;
        #endregion

        #region Dữ liệu Mở rộng (Biến thể, EAV, Khuyến mãi)
        /// <summary>Danh sách đầy đủ các thuộc tính EAV (Đáp ứng yêu cầu minh bạch nông sản theo NĐ 15/2018/NĐ-CP).</summary>
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

        /// <summary>Danh sách các phân loại / quy cách đóng gói (SKUs) để khách hàng lựa chọn.</summary>
        public List<ShopProductVariantDto> Variants { get; set; } = new List<ShopProductVariantDto>();

        /// <summary>Danh sách các tem nhãn khuyến mãi đang áp dụng cho sản phẩm này.</summary>
        public List<ShopPromotionBadgeDto> ActivePromotions { get; set; } = new List<ShopPromotionBadgeDto>();
        #endregion
    }

    /// <summary>
    /// DTO hiển thị Biến thể (SKU) trong trang chi tiết sản phẩm.
    /// </summary>
    public class ShopProductVariantDto
    {
        public int Id { get; set; }

        #region Thông tin Biến thể
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        #endregion

        #region Giá, Thuộc tính & Tồn kho
        /// <summary>Các tùy chọn giá bán theo Đơn vị tính (Ví dụ: Mua lẻ theo Kg, mua sỉ theo Thùng).</summary>
        public List<ShopVariantPriceDto> Prices { get; set; } = new List<ShopVariantPriceDto>();

        /// <summary>Thuộc tính riêng của biến thể này (nếu có).</summary>
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

        /// <summary>Số lượng tối đa khách có thể thêm vào giỏ hàng.</summary>
        public decimal QuantityAvailable { get; set; }
        public bool IsInStock { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO hiển thị Tùy chọn Giá bán theo ĐVT (Nằm trong Biến thể).
    /// </summary>
    public class ShopVariantPriceDto
    {
        #region Thông tin Giá
        public int PriceId { get; set; }

        /// <summary>Mã Đơn vị tính (Dùng để submit khi thêm vào giỏ).</summary>
        public int UoMId { get; set; }

        /// <summary>Tên Đơn vị tính hiển thị cho khách (Ví dụ: 1 Kg, Thùng 10Kg).</summary>
        public string UoMName { get; set; } = string.Empty;

        /// <summary>Giá gốc (bị gạch ngang nếu có giảm giá).</summary>
        public decimal Price { get; set; }

        /// <summary>Giá thực tế phải trả.</summary>
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountPercent { get; set; }

        /// <summary>Là tùy chọn giá được chọn sẵn khi vừa load trang.</summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Hệ số quy đổi so với ĐVT cơ sở (Ví dụ: 1 Thùng = 10 Kg -> Factor = 10).
        /// </summary>
        public decimal ConversionFactor { get; set; } = 1;

        /// <summary>
        /// Chuỗi hiển thị quy đổi trực quan cho khách (Ví dụ: "1 Thùng = 10 Kilogram").
        /// </summary>
        public string? ConversionText { get; set; }
        #endregion
    }

    /// <summary>
    /// DTO hiển thị Tem Khuyến mãi (Badge/Tag).
    /// </summary>
    public class ShopPromotionBadgeDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }

        /// <summary>Cách thức giảm (true: Giảm theo %, false: Giảm số tiền trực tiếp).</summary>
        public bool IsPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// DTO hiển thị Dòng sản phẩm (Cấp 3) trong cây danh mục đa tầng.
    /// </summary>
    public class ShopProductCategoryItemDto
    {
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string ProductSlug { get; set; }
        public int VariantCount { get; set; }
    }

    /// <summary>
    /// DTO hiển thị Danh mục con trên Menu Navigation (Cấp 2).
    /// </summary>
    public class ShopCategoryItemDto
    {
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public required string CategorySlug { get; set; }
        public string? CategoryImage { get; set; }

        /// <summary>Số lượng sản phẩm thuộc danh mục này.</summary>
        public int ProductCount { get; set; }

        /// <summary>Danh sách các dòng sản phẩm (Cấp 3) trực thuộc loại sản phẩm này.</summary>
        public List<ShopProductCategoryItemDto> Products { get; set; } = new List<ShopProductCategoryItemDto>();
    }

    /// <summary>
    /// DTO hiển thị Cấu trúc Cây Menu (Nhóm ngành hàng -> Danh mục con -> Dòng sản phẩm).
    /// </summary>
    public class ShopCategoryTreeDto
    {
        #region Thông tin Nhóm Ngành Hàng
        public int GroupId { get; set; }
        public required string GroupName { get; set; }
        public required string GroupSlug { get; set; }
        public string? GroupImage { get; set; }
        #endregion

        /// <summary>Danh sách các danh mục trực thuộc.</summary>
        public List<ShopCategoryItemDto> Categories { get; set; } = new List<ShopCategoryItemDto>();
    }

    /// <summary>
    /// Parameters dùng để Lọc và Tìm kiếm sản phẩm tại Cửa hàng.
    /// Truyền qua Query String trên URL.
    /// </summary>
    public class ShopProductFilterParams
    {
        #region Từ khóa & Danh mục Đa Tầng
        public string? Search { get; set; }
        public string? CategoryGroupSlug { get; set; }
        public string? CategorySlug { get; set; }
        public string? ProductSlug { get; set; }
        #endregion

        #region Bộ lọc Động (Giá & Thuộc tính EAV)
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Origin { get; set; }
        public string? Certification { get; set; }
        #endregion

        #region Sắp xếp & Phân trang
        /// <summary>Quy tắc sắp xếp (Hỗ trợ: price-asc, price-desc, name-asc, newest, discount).</summary>
        public string? SortBy { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        #endregion
    }

    /// <summary>
    /// DTO hiển thị Trang Landing Page của một Chương trình Khuyến mãi cụ thể.
    /// </summary>
    public class ShopPromotionDetailDto
    {
        public int Id { get; set; }

        #region Thông tin Chương trình
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? Description { get; set; }

        /// <summary>Ảnh bìa lớn cho chiến dịch.</summary>
        public string? BannerImagePath { get; set; }
        #endregion

        #region Cấu hình Giảm giá & Thời gian
        public bool IsPercentage { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        #endregion

        /// <summary>Danh sách các sản phẩm áp dụng trong chương trình này.</summary>
        public List<ShopProductCardDto> Products { get; set; } = new List<ShopProductCardDto>();
    }
}