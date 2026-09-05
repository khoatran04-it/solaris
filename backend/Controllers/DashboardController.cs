using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Dashboard 1: Tổng quan Kinh doanh & Doanh thu
        /// period: today | 7days | 30days | year
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(
            [FromQuery] string period = "30days",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _dashboardService.GetOverviewAsync(period, fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 2: Doanh số & Phân bổ Địa lý
        /// </summary>
        [HttpGet("sales-geography")]
        public async Task<IActionResult> GetSalesGeography(
            [FromQuery] string period = "30days",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _dashboardService.GetSalesGeographyAsync(period, fromDate, toDate);
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 3: Tồn kho & Sức chứa Kho hàng
        /// </summary>
        [HttpGet("inventory-capacity")]
        public async Task<IActionResult> GetInventoryCapacity()
        {
            var result = await _dashboardService.GetInventoryCapacityAsync();
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 4: Chất lượng & Hạn dùng Nông sản (FEFO)
        /// </summary>
        [HttpGet("quality-expiry")]
        public async Task<IActionResult> GetQualityExpiry()
        {
            var result = await _dashboardService.GetQualityExpiryAsync();
            return Ok(result);
        }
    }
}
