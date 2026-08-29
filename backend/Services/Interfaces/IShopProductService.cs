using backend.DTOs;
using backend.DTOs.ShopDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service cung cấp dữ liệu Cửa hàng (B2C / Front-end).
    /// Các hàm trong này chỉ truy vấn và trả về các sản phẩm/danh mục đang ở trạng thái Hoạt động (IsActive = true).
    /// </summary>
    public interface IShopProductService
    {
        #region Danh mục & Menu (Categories)
        /// <summary>
        /// Lấy cấu trúc Cây danh mục (Nhóm ngành hàng -> Danh mục con).
        /// Dùng để render Menu Navigation chính trên Header hoặc Sidebar của Cửa hàng.
        /// </summary>
        Task<List<ShopCategoryTreeDto>> GetCategoryTreeAsync();
        #endregion

        #region Truy vấn Danh sách Sản phẩm (Product Lists)
        /// <summary>
        /// Lấy danh sách thẻ sản phẩm (Product Card) có phân trang dựa trên bộ lọc phức hợp.
        /// Dùng cho Trang chủ, Trang danh mục, hoặc Trang kết quả tìm kiếm.
        /// </summary>
        /// <param name="filter">Đối tượng chứa các tham số lọc (từ khóa, giá, danh mục, vùng trồng...).</param>
        Task<PagedResult<ShopProductCardDto>> GetProductsAsync(ShopProductFilterParams filter);

        /// <summary>
        /// Lấy danh sách các sản phẩm Nổi bật (Featured).
        /// Thường là các sản phẩm có lượt mua cao hoặc được admin đánh dấu nổi bật để hiển thị trên Trang chủ.
        /// </summary>
        /// <param name="limit">Số lượng sản phẩm tối đa cần lấy (mặc định 8).</param>
        Task<List<ShopProductCardDto>> GetFeaturedProductsAsync(int limit = 8);

        /// <summary>
        /// Lấy danh sách Hàng mới về (New Arrivals).
        /// Dựa trên ngày tạo sản phẩm gần nhất.
        /// </summary>
        /// <param name="limit">Số lượng sản phẩm tối đa cần lấy (mặc định 8).</param>
        Task<List<ShopProductCardDto>> GetNewArrivalsAsync(int limit = 8);
        #endregion

        #region Chi tiết Sản phẩm (Product Details)
        /// <summary>
        /// Lấy toàn bộ thông tin chi tiết của một sản phẩm (bao gồm EAV, Biến thể, Giá, Khuyến mãi).
        /// Dùng để render Trang Chi tiết Sản phẩm (PDP - Product Detail Page) thông qua đường dẫn thân thiện (Slug).
        /// </summary>
        /// <param name="slug">Đường dẫn SEO của sản phẩm (Ví dụ: tao-envy-new-zealand).</param>
        /// <returns>Chi tiết sản phẩm hoặc null nếu không tìm thấy/sản phẩm đã bị ẩn.</returns>
        Task<ShopProductDetailDto?> GetProductBySlugAsync(string slug);
        #endregion

        #region Khuyến mãi & Chiến dịch (Promotions)
        /// <summary>
        /// Lấy danh sách các chương trình khuyến mãi đang diễn ra (Active).
        /// Dùng để hiển thị Banner, Flash Sale hoặc danh sách Voucher trang chủ.
        /// </summary>
        Task<List<ShopPromotionBadgeDto>> GetActivePromotionsAsync();

        /// <summary>
        /// Lấy chi tiết một chương trình khuyến mãi và danh sách các sản phẩm thuộc chương trình đó.
        /// Dùng để render Landing Page cho từng chiến dịch (Ví dụ: Mừng Đại Lễ, Black Friday).
        /// </summary>
        /// <param name="slug">Đường dẫn SEO của chương trình khuyến mãi.</param>
        Task<ShopPromotionDetailDto?> GetPromotionBySlugAsync(string slug);
        #endregion

        #region Dữ liệu Bộ lọc Động (Dynamic Filters)
        /// <summary>
        /// Lấy danh sách các Vùng trồng (Origin) duy nhất từ các sản phẩm đang bán.
        /// Dùng để tạo các Checkbox lọc vùng miền bên Sidebar (Ví dụ: Đà Lạt, Mộc Châu, Nhập khẩu).
        /// </summary>
        Task<List<string>> GetAvailableOriginsAsync();

        /// <summary>
        /// Lấy danh sách các Chứng nhận chất lượng (Certification) duy nhất từ các sản phẩm đang bán.
        /// Dùng để tạo các Checkbox lọc chứng nhận (Ví dụ: VietGAP, GlobalGAP, Organic).
        /// </summary>
        Task<List<string>> GetAvailableCertificationsAsync();
        #endregion
    }
}