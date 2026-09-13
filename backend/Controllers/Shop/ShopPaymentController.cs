using backend.DTOs.PaymentDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [Route("api/shop/payment")]
    [ApiController]
    public class ShopPaymentController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;

        public ShopPaymentController(IVnPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        /// <summary>
        /// Sinh URL thanh toán VNPay Sandbox cho đơn hàng
        /// </summary>
        [HttpPost("vnpay/create-url")]
        public async Task<IActionResult> CreateVnPayUrl([FromBody] VnPayPaymentRequestDto request)
        {
            try
            {
                var response = await _vnPayService.CreatePaymentUrlAsync(request, HttpContext);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi tạo URL thanh toán VNPay.", details = ex.Message });
            }
        }

        /// <summary>
        /// Xử lý dữ liệu trả về từ VNPay khi người dùng hoàn tất thanh toán
        /// </summary>
        [HttpGet("vnpay/callback")]
        public async Task<IActionResult> VnPayCallback()
        {
            try
            {
                var result = await _vnPayService.ProcessCallbackAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi xử lý phản hồi VNPay.", details = ex.Message });
            }
        }

        /// <summary>
        /// IPN Webhook hai chiều từ máy chủ VNPay
        /// </summary>
        [HttpGet("vnpay/ipn")]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                var ipnResult = await _vnPayService.ProcessIpnAsync(Request.Query);
                return Ok(ipnResult);
            }
            catch (Exception ex)
            {
                return Ok(new VnPayIpnResponseDto { RspCode = "99", Message = $"Unknown error: {ex.Message}" });
            }
        }
    }
}
