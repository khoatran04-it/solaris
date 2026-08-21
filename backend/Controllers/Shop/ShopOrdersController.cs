using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/orders")]
    [Authorize]
    public class ShopOrdersController : ShopBaseController
    {
        private readonly IShopOrderService _orderService;

        public ShopOrdersController(IShopOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Tiến hành đặt hàng từ giỏ hàng hiện tại (Checkout)
        /// </summary>
        [HttpPost("checkout")]
        public async Task<ActionResult<ShopOrderReadDto>> Checkout([FromBody] ShopCheckoutRequestDto request)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _orderService.CheckoutAsync(customerId, request);
            return Ok(order);
        }

        /// <summary>
        /// Lịch sử đơn hàng của khách hàng (phân trang)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<ShopOrderReadDto>>> GetOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            int customerId = GetCurrentCustomerId();
            var orders = await _orderService.GetCustomerOrdersAsync(customerId, pageIndex, pageSize);
            return Ok(orders);
        }

        /// <summary>
        /// Chi tiết 1 đơn hàng theo mã đơn
        /// </summary>
        [HttpGet("{orderCode}")]
        public async Task<ActionResult<ShopOrderReadDto>> GetOrderByCode(string orderCode)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _orderService.GetOrderByCodeAsync(customerId, orderCode);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            return Ok(order);
        }

        /// <summary>
        /// Hủy đơn hàng (khi chưa giao)
        /// </summary>
        [HttpPost("{orderCode}/cancel")]
        public async Task<ActionResult> CancelOrder(string orderCode, [FromBody] ShopOrderCancelRequestDto request)
        {
            int customerId = GetCurrentCustomerId();
            await _orderService.CancelOrderAsync(customerId, orderCode, request.Reason);
            return Ok(new { message = "Đã hủy đơn hàng và hoàn trả tồn kho thành công." });
        }
    }
}
