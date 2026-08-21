using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/products")]
    public class ShopProductsController : ShopBaseController
    {
        private readonly IShopProductService _productService;

        public ShopProductsController(IShopProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Danh sách sản phẩm kèm bộ lọc đa tầng & phân trang
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<ShopProductCardDto>>> GetProducts([FromQuery] ShopProductFilterParams filter)
        {
            var result = await _productService.GetProductsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Chi tiết sản phẩm theo Slug (cho SEO)
        /// </summary>
        [HttpGet("{slug}")]
        public async Task<ActionResult<ShopProductDetailDto>> GetProductBySlug(string slug)
        {
            var product = await _productService.GetProductBySlugAsync(slug);
            if (product == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(product);
        }

        /// <summary>
        /// Cây danh mục phân cấp Nhóm -> Danh mục con (kèm số lượng SP)
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<ShopCategoryTreeDto>>> GetCategoryTree()
        {
            var tree = await _productService.GetCategoryTreeAsync();
            return Ok(tree);
        }

        /// <summary>
        /// Sản phẩm nổi bật / Khuyến mãi nhiều nhất
        /// </summary>
        [HttpGet("featured")]
        public async Task<ActionResult<List<ShopProductCardDto>>> GetFeatured([FromQuery] int limit = 8)
        {
            var items = await _productService.GetFeaturedProductsAsync(limit);
            return Ok(items);
        }

        /// <summary>
        /// Sản phẩm tươi mới vừa về
        /// </summary>
        [HttpGet("new-arrivals")]
        public async Task<ActionResult<List<ShopProductCardDto>>> GetNewArrivals([FromQuery] int limit = 8)
        {
            var items = await _productService.GetNewArrivalsAsync(limit);
            return Ok(items);
        }

        /// <summary>
        /// Danh sách các chiến dịch khuyến mãi đang diễn ra
        /// </summary>
        [HttpGet("promotions")]
        public async Task<ActionResult<List<ShopPromotionBadgeDto>>> GetActivePromotions()
        {
            var promos = await _productService.GetActivePromotionsAsync();
            return Ok(promos);
        }

        /// <summary>
        /// Chi tiết chương trình khuyến mãi và danh sách SP áp dụng theo Slug
        /// </summary>
        [HttpGet("promotions/{slug}")]
        public async Task<ActionResult<ShopPromotionDetailDto>> GetPromotionBySlug(string slug)
        {
            var promo = await _productService.GetPromotionBySlugAsync(slug);
            if (promo == null)
                return NotFound(new { message = "Không tìm thấy chương trình khuyến mãi." });

            return Ok(promo);
        }

        /// <summary>
        /// Danh sách các vùng trồng / xuất xứ có sẵn để làm bộ lọc
        /// </summary>
        [HttpGet("origins")]
        public async Task<ActionResult<List<string>>> GetAvailableOrigins()
        {
            var origins = await _productService.GetAvailableOriginsAsync();
            return Ok(origins);
        }

        /// <summary>
        /// Danh sách các tiêu chuẩn chứng nhận có sẵn để làm bộ lọc
        /// </summary>
        [HttpGet("certifications")]
        public async Task<ActionResult<List<string>>> GetAvailableCertifications()
        {
            var certs = await _productService.GetAvailableCertificationsAsync();
            return Ok(certs);
        }
    }
}
