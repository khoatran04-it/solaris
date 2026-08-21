using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/cart")]
    [Authorize]
    public class ShopCartController : ShopBaseController
    {
        private readonly IShopCartService _cartService;

        public ShopCartController(IShopCartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Lấy thông tin giỏ hàng của khách hàng
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ShopCartDto>> GetCart()
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.GetCartAsync(customerId);
            return Ok(cart);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        [HttpPost("items")]
        public async Task<ActionResult<ShopCartDto>> AddItem([FromBody] ShopCartAddDto request)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.AddItemAsync(customerId, request);
            return Ok(cart);
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm trong giỏ hàng
        /// </summary>
        [HttpPut("items/{cartItemId}")]
        public async Task<ActionResult<ShopCartDto>> UpdateQuantity(int cartItemId, [FromBody] ShopCartUpdateDto request)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.UpdateItemQuantityAsync(customerId, cartItemId, request.Quantity);
            return Ok(cart);
        }

        /// <summary>
        /// Xóa 1 sản phẩm khỏi giỏ hàng
        /// </summary>
        [HttpDelete("items/{cartItemId}")]
        public async Task<ActionResult<ShopCartDto>> RemoveItem(int cartItemId)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.RemoveItemAsync(customerId, cartItemId);
            return Ok(cart);
        }

        /// <summary>
        /// Xóa sạch giỏ hàng
        /// </summary>
        [HttpDelete]
        public async Task<ActionResult<ShopCartDto>> ClearCart()
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.ClearCartAsync(customerId);
            return Ok(cart);
        }

        /// <summary>
        /// Đồng bộ giỏ hàng từ khách vãng lai (LocalStorage) sau khi đăng nhập
        /// </summary>
        [HttpPost("sync")]
        public async Task<ActionResult<ShopCartDto>> SyncGuestCart([FromBody] ShopSyncGuestCartDto request)
        {
            int customerId = GetCurrentCustomerId();
            var cart = await _cartService.SyncGuestCartAsync(customerId, request);
            return Ok(cart);
        }
    }
}
