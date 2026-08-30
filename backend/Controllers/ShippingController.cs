using backend.DTOs.ShippingDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/shipping")]
    [ApiController]
    [Produces("application/json")]
    public class ShippingController : ControllerBase
    {
        private readonly IGhnService _ghnService;

        public ShippingController(IGhnService ghnService)
        {
            _ghnService = ghnService;
        }

        /// <summary>
        /// Lấy danh sách Tỉnh / Thành phố chuẩn từ GHN Master Data
        /// </summary>
        [HttpGet("provinces")]
        [ProducesResponseType(typeof(List<GhnProvinceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProvinces()
        {
            var data = await _ghnService.GetProvincesAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách Quận / Huyện theo Tỉnh / Thành phố từ GHN Master Data
        /// </summary>
        [HttpGet("districts/{provinceId}")]
        [ProducesResponseType(typeof(List<GhnDistrictDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var data = await _ghnService.GetDistrictsAsync(provinceId);
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách Phường / Xã theo Quận / Huyện từ GHN Master Data
        /// </summary>
        [HttpGet("wards/{districtId}")]
        [ProducesResponseType(typeof(List<GhnWardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var data = await _ghnService.GetWardsAsync(districtId);
            return Ok(data);
        }

        /// <summary>
        /// Tính phí vận chuyển GHN thời gian thực kèm chính sách Freeship 300k
        /// </summary>
        [HttpPost("calculate-fee")]
        [ProducesResponseType(typeof(GhnCalculateFeeResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalculateFee([FromBody] GhnCalculateFeeRequestDto request)
        {
            var result = await _ghnService.CalculateShippingFeeAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Nhân viên kho/admin bấm đẩy đơn hàng sang Giao Hàng Nhanh (GHN)
        /// </summary>
        [HttpPost("ghn/create-order/{orderId}")]
        [ProducesResponseType(typeof(GhnCreateOrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
