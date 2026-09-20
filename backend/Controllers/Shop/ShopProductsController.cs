using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    /// <summary>
    /// API cung cấp dữ liệu Cửa hàng (Shop B2C/B2B Front-end).
    /// </summary>
    [ApiController]
    [Route("api/shop/products")]
    [Produces("application/json")]
    public class ShopProductsController : ShopBaseController
    {
        private readonly IShopProductService _productService;

        public ShopProductsController(IShopProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Danh sách sản phẩm kèm bộ lọc đa tầng & phân trang.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ShopProductCardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<ShopProductCardDto>>> GetProducts([FromQuery] ShopProductFilterParams filter)
        {
            var result = await _productService.GetProductsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Chi tiết sản phẩm theo Slug (cho SEO) hoặc ID.
        /// </summary>
        [HttpGet("{slug}")]
        [ProducesResponseType(typeof(ShopProductDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ShopProductDetailDto>> GetProductBySlug(string slug, [FromQuery] int? warehouseId = null)
        {
            var product = await _productService.GetProductBySlugAsync(slug, warehouseId);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(product);
        }

        /// <summary>
        /// Cây danh mục phân cấp Nhóm -> Danh mục con (kèm số lượng SP).
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<ShopCategoryTreeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ShopCategoryTreeDto>>> GetCategoryTree()
        {
            var tree = await _productService.GetCategoryTreeAsync();
            return Ok(tree);
        }

        /// <summary>
        /// Sản phẩm nổi bật (Mua nhiều nhất / Bán chạy nhất từ database).
        /// </summary>
        [HttpGet("featured")]
        [ProducesResponseType(typeof(List<ShopProductCardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ShopProductCardDto>>> GetFeatured([FromQuery] int limit = 20, [FromQuery] int? warehouseId = null)
        {
            var items = await _productService.GetFeaturedProductsAsync(limit, warehouseId);
            return Ok(items);
        }

        /// <summary>
        /// Sản phẩm tươi mới vừa về (20 sản phẩm mới nhất).
        /// </summary>
        [HttpGet("new-arrivals")]
        [ProducesResponseType(typeof(List<ShopProductCardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ShopProductCardDto>>> GetNewArrivals([FromQuery] int limit = 20, [FromQuery] int? warehouseId = null)
        {
            var items = await _productService.GetNewArrivalsAsync(limit, warehouseId);
            return Ok(items);
        }

        /// <summary>
        /// Sản phẩm ưu đãi hôm nay (thật sự có trong chiến dịch khuyến mãi đang diễn ra).
        /// </summary>
        [HttpGet("deals")]
        [ProducesResponseType(typeof(List<ShopProductCardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ShopProductCardDto>>> GetDeals([FromQuery] int limit = 20, [FromQuery] int? warehouseId = null)
        {
            var items = await _productService.GetDiscountedProductsAsync(limit, warehouseId);
            return Ok(items);
        }

        /// <summary>
        /// Danh sách các chiến dịch khuyến mãi đang diễn ra.
        /// </summary>
        [HttpGet("promotions")]
        [ProducesResponseType(typeof(List<ShopPromotionBadgeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ShopPromotionBadgeDto>>> GetActivePromotions()
        {
            var promos = await _productService.GetActivePromotionsAsync();
            return Ok(promos);
        }

        /// <summary>
        /// Chi tiết chương trình khuyến mãi và danh sách SP áp dụng theo Slug.
        /// </summary>
        [HttpGet("promotions/{slug}")]
        [ProducesResponseType(typeof(ShopPromotionDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ShopPromotionDetailDto>> GetPromotionBySlug(string slug)
        {
            var promo = await _productService.GetPromotionBySlugAsync(slug);
            if (promo == null)
                return NotFound(new { message = "Không tìm thấy chương trình khuyến mãi." });

            return Ok(promo);
        }

        /// <summary>
        /// Danh sách các vùng trồng / xuất xứ có sẵn để làm bộ lọc.
        /// </summary>
        [HttpGet("origins")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<string>>> GetAvailableOrigins()
        {
            var origins = await _productService.GetAvailableOriginsAsync();
            return Ok(origins);
        }

        /// <summary>
        /// Danh sách các tiêu chuẩn chứng nhận có sẵn để làm bộ lọc.
        /// </summary>
        [HttpGet("certifications")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<string>>> GetAvailableCertifications()
        {
            var certs = await _productService.GetAvailableCertificationsAsync();
            return Ok(certs);
        }
    }
}
