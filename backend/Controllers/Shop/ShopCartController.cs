using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace backend.Controllers.Shop
{
    /// <summary>
    /// API Quản Lý Giỏ Hàng Mua Sắm Khách Hàng (Module 12 - Shopping Cart API).
    /// Cung cấp các điểm cuối phục vụ: Xem giỏ hàng, Thêm sản phẩm, Cập nhật số lượng, Xóa món, Làm sạch giỏ và Đồng bộ giỏ hàng vãng lai.
    /// </summary>
    [ApiController]
    [Route("api/shop/cart")]
    [Produces("application/json")]
    [Authorize]
    public class ShopCartController : ShopBaseController
    {
        private readonly IShopCartService _cartService;

        public ShopCartController(IShopCartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Lấy toàn bộ thông tin chi tiết giỏ hàng hiện tại của Khách hàng.
        /// </summary>
        /// <remarks>
        /// Hệ thống sẽ tự động tính toán lại tồn kho khả dụng (Available Stock), đơn giá theo ĐVT, và các chương trình khuyến mãi đang áp dụng.
        /// </remarks>
        /// <response code="200">Trả về thông tin giỏ hàng kèm danh sách sản phẩm và tổng tiền thanh toán.</response>
        /// <response code="401">Khách hàng chưa đăng nhập hoặc token đã hết hạn.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ShopCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopCartDto>> GetCart()
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.GetCartAsync(customerId);
            return Ok(cart);
        }

        /// <summary>
        /// Thêm một sản phẩm (Variant) với Đơn vị tính (UoM) và số lượng cụ thể vào giỏ hàng.
        /// </summary>
        /// <remarks>
        /// Nếu sản phẩm với đúng ĐVT đã tồn tại trong giỏ, hệ thống sẽ tự động cộng dồn số lượng.
        /// </remarks>
        /// <param name="request">Thông tin sản phẩm cần thêm (VariantId, UoMId, Quantity).</param>
        /// <response code="200">Thêm thành công, trả về giỏ hàng mới nhất.</response>
        /// <response code="400">Dữ liệu yêu cầu không hợp lệ (Số lượng &lt;= 0).</response>
        /// <response code="404">Không tìm thấy sản phẩm hoặc đơn vị tính.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [HttpPost("items")]
        [ProducesResponseType(typeof(ShopCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopCartDto>> AddItem([FromBody] ShopCartAddDto request)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.AddItemAsync(customerId, request);
            return Ok(cart);
        }

        /// <summary>
        /// Cập nhật số lượng của một dòng sản phẩm cụ thể trong giỏ hàng.
        /// </summary>
        /// <remarks>
        /// Nếu số lượng truyền lên bằng 0 hoặc âm, dòng sản phẩm đó sẽ tự động bị xóa khỏi giỏ hàng.
        /// </remarks>
        /// <param name="cartItemId">ID dòng sản phẩm trong giỏ (ShoppingCartItem.Id).</param>
        /// <param name="request">Thông tin cập nhật (Quantity mới).</param>
        /// <response code="200">Cập nhật thành công, trả về giỏ hàng mới nhất.</response>
        /// <response code="404">Không tìm thấy dòng sản phẩm trong giỏ hàng.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [HttpPut("items/{cartItemId}")]
        [ProducesResponseType(typeof(ShopCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopCartDto>> UpdateQuantity(int cartItemId, [FromBody] ShopCartUpdateDto request)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.UpdateItemQuantityAsync(customerId, cartItemId, request.Quantity);
            return Ok(cart);
        }

        /// <summary>
        /// Xóa một dòng sản phẩm khỏi giỏ hàng.
        /// </summary>
        /// <param name="cartItemId">ID dòng sản phẩm trong giỏ cần xóa (ShoppingCartItem.Id).</param>
        /// <response code="200">Xóa thành công, trả về giỏ hàng mới nhất.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [HttpDelete("items/{cartItemId}")]
        [ProducesResponseType(typeof(ShopCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopCartDto>> RemoveItem(int cartItemId)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.RemoveItemAsync(customerId, cartItemId);
            return Ok(cart);
        }

        /// <summary>
        /// Làm sạch toàn bộ giỏ hàng (Xóa toàn bộ sản phẩm).
        /// </summary>
        /// <response code="200">Làm sạch giỏ hàng thành công, trả về giỏ hàng rỗng.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [HttpDelete]
        [ProducesResponseType(typeof(ShopCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopCartDto>> ClearCart()
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.ClearCartAsync(customerId);
            return Ok(cart);
        }

        /// <summary>
        /// Đồng bộ và gộp giỏ hàng từ khách vãng lai (lưu tại LocalStorage trên trình duyệt) vào tài khoản sau khi đăng nhập.
        /// </summary>
        /// <param name="request">Danh sách các món hàng vãng lai kèm số lượng.</param>
        /// <response code="200">Đồng bộ thành công, trả về giỏ hàng hợp nhất.</response>
        /// <response code="401">Chưa đăng nhập.</response>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(ShopCartDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopCartDto>> SyncGuestCart([FromBody] ShopSyncGuestCartDto request)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.SyncGuestCartAsync(customerId, request);
            return Ok(cart);
        }
    }
}
