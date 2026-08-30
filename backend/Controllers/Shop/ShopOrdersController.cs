using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers.Shop
{
    /// <summary>
    /// API Quản lý Đơn Mua Hàng Khách Hàng (Customer Storefront Orders & Checkout).
    /// Hỗ trợ đặt hàng từ giỏ hàng hiện tại (Checkout), tra cứu lịch sử mua sắm cá nhân,
    /// xem chi tiết đơn hàng theo mã công khai và tự hủy đơn khi kho chưa đóng gói.
    /// </summary>
    [ApiController]
    [Route("api/shop/orders")]
    [Produces("application/json")]
    [Authorize]
    public class ShopOrdersController : ShopBaseController
    {
        private readonly IShopOrderService _orderService;

        public ShopOrdersController(IShopOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Tiến hành Đặt hàng từ Giỏ hàng hiện tại (Checkout).
        /// </summary>
        /// <param name="request">Thông tin giao hàng, địa chỉ, phương thức thanh toán và ghi chú đặt hàng.</param>
        /// <returns>Dữ liệu đơn hàng vừa được khởi tạo thành công cùng mã tra cứu đơn.</returns>
        /// <response code="200">Đặt hàng và giữ chỗ tồn kho thành công.</response>
        /// <response code="400">Giỏ hàng rỗng, địa chỉ không hợp lệ hoặc số lượng tồn kho khả dụng không đủ.</response>
        /// <response code="404">Không tìm thấy thông tin tài khoản khách hàng.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ShopOrderReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopOrderReadDto>> Checkout([FromBody] ShopCheckoutRequestDto request)
        {
            try
            {
                int customerId = GetCurrentCustomerId();
                var order = await _orderService.CheckoutAsync(customerId, request);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách Lịch sử đơn hàng của khách hàng hiện tại (có phân trang).
        /// </summary>
        /// <param name="pageIndex">Số thứ tự trang (mặc định 1).</param>
        /// <param name="pageSize">Số đơn hàng trên mỗi trang (mặc định 10).</param>
        /// <returns>Danh sách đơn hàng của khách hàng kèm thông tin phân trang.</returns>
        /// <response code="200">Truy vấn lịch sử đơn hàng thành công.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ShopOrderReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<ShopOrderReadDto>>> GetOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            int customerId = GetCurrentCustomerId();
            var orders = await _orderService.GetCustomerOrdersAsync(customerId, pageIndex, pageSize);
            return Ok(orders);
        }

        /// <summary>
        /// Xem chi tiết một Đơn hàng theo Mã đơn hàng công khai (OrderCode).
        /// </summary>
        /// <param name="orderCode">Mã đơn hàng (ví dụ: ORD-20260830-ABC123).</param>
        /// <returns>Thông tin chi tiết đơn hàng kèm danh sách các mặt hàng đã mua.</returns>
        /// <response code="200">Tìm thấy và trả về chi tiết đơn hàng.</response>
        /// <response code="404">Không tìm thấy đơn hàng hoặc đơn hàng không thuộc về khách hàng hiện tại.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpGet("{orderCode}")]
        [ProducesResponseType(typeof(ShopOrderReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopOrderReadDto>> GetOrderByCode(string orderCode)
        {
            int customerId = GetCurrentCustomerId();
            var order = await _orderService.GetOrderByCodeAsync(customerId, orderCode);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn hàng." });

            return Ok(order);
        }

        /// <summary>
        /// Khách hàng tự yêu cầu Hủy đơn hàng (Áp dụng khi đơn hàng chưa được xuất kho giao đi).
        /// </summary>
        /// <param name="orderCode">Mã đơn hàng cần hủy.</param>
        /// <param name="request">Đối tượng chứa lý do hủy đơn của khách hàng.</param>
        /// <returns>Thông báo kết quả hủy đơn hàng và hoàn trả tồn kho.</returns>
        /// <response code="200">Hủy đơn hàng và hoàn trả tồn kho thành công.</response>
        /// <response code="400">Đơn hàng đang ở trạng thái chuẩn bị/giao hàng nên không thể tự hủy.</response>
        /// <response code="404">Không tìm thấy đơn hàng cần hủy.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpPost("{orderCode}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> CancelOrder(string orderCode, [FromBody] ShopOrderCancelRequestDto request)
        {
            try
            {
                int customerId = GetCurrentCustomerId();
                await _orderService.CancelOrderAsync(customerId, orderCode, request.Reason);
                return Ok(new { message = "Đã hủy đơn hàng và hoàn trả tồn kho thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
