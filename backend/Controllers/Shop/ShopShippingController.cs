using backend.DTOs.ShippingDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [Route("api/shop/shipping")]
    [ApiController]
    public class ShopShippingController : ControllerBase
    {
        private readonly IGhnService _ghnService;

        public ShopShippingController(IGhnService ghnService)
        {
            _ghnService = ghnService;
        }

        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var data = await _ghnService.GetProvincesAsync();
            return Ok(data);
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var data = await _ghnService.GetDistrictsAsync(provinceId);
            return Ok(data);
        }

        [HttpGet("wards/{districtId}")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var data = await _ghnService.GetWardsAsync(districtId);
            return Ok(data);
        }

        [HttpPost("calculate-fee")]
        public async Task<IActionResult> CalculateFee([FromBody] GhnCalculateFeeRequestDto request)
        {
            var result = await _ghnService.CalculateShippingFeeAsync(request);
            return Ok(result);
        }
    }
}
