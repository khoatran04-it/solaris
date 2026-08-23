using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/shipping")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IGhnService _ghnService;

        public ShippingController(IGhnService ghnService)
        {
            _ghnService = ghnService;
        }

        /// <summary>
        /// Nhân viên kho/admin bấm đẩy đơn hàng sang Giao Hàng Nhanh (GHN)
        /// </summary>
        [HttpPost("ghn/create-order/{orderId}")]
        public async Task<IActionResult> CreateGhnOrder(int orderId)
        {
            try
            {
                var result = await _ghnService.CreateShippingOrderAsync(orderId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi đẩy đơn sang GHN.", details = ex.Message });
            }
        }
    }
}
