using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        #region Helper: Phân quyền & Kho được phép truy cập
        private bool HasPermission(string permissionCode)
        {
            if (User.IsInRole("SUPER_ADMIN") || User.IsInRole("ADMIN"))
            {
                return true;
            }

            return User.Claims.Any(c => c.Type == "Permission" && c.Value == permissionCode);
        }

        private List<int>? GetAllowedWarehouseIds()
        {
            if (User.IsInRole("SUPER_ADMIN") || User.IsInRole("ADMIN"))
            {
                return null;
            }

            var claim = User.Claims.FirstOrDefault(c => c.Type == "WarehouseIds");
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
            }

            return null;
        }
        #endregion

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
            if (!HasPermission("DASHBOARD_OVERVIEW"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem Báo cáo Tổng quan Kinh doanh." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetOverviewAsync(period, fromDate, toDate, allowedWarehouseIds);
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
            if (!HasPermission("DASHBOARD_SALES"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem Báo cáo Doanh số & Khách hàng." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetSalesGeographyAsync(period, fromDate, toDate, allowedWarehouseIds);
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 3: Tồn kho & Sức chứa Kho hàng
        /// </summary>
        [HttpGet("inventory-capacity")]
        public async Task<IActionResult> GetInventoryCapacity()
        {
            if (!HasPermission("DASHBOARD_INVENTORY"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem Báo cáo Tồn kho & Sức chứa." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetInventoryCapacityAsync(allowedWarehouseIds);
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 4: Chất lượng & Hạn dùng Nông sản (FEFO)
        /// </summary>
        [HttpGet("quality-expiry")]
        public async Task<IActionResult> GetQualityExpiry()
        {
            if (!HasPermission("DASHBOARD_QUALITY"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem Báo cáo Chất lượng & Hạn dùng Nông sản." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetQualityExpiryAsync(allowedWarehouseIds);
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 5: Tài chính, Giá vốn & Dòng tiền Nhập - Bán
        /// </summary>
        [HttpGet("financial-performance")]
        public async Task<IActionResult> GetFinancialPerformance(
            [FromQuery] string period = "30days",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (!HasPermission("DASHBOARD_FINANCE"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem Báo cáo Tài chính & Dòng tiền." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetFinancialPerformanceAsync(period, fromDate, toDate, allowedWarehouseIds);
            return Ok(result);
        }

        /// <summary>
        /// Dashboard 6: Phân tích Biến động Giá Nhập vs Giá Bán theo SKU & Thời gian
        /// timeframe: week | month | year
        /// </summary>
        [HttpGet("price-volatility")]
        public async Task<IActionResult> GetPriceVolatility(
            [FromQuery] int? variantId = null,
            [FromQuery] string timeframe = "month",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (!HasPermission("DASHBOARD_PRICE"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền xem Báo cáo Biến động Giá & Biên lãi." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetPriceVolatilityAsync(variantId, timeframe, fromDate, toDate, allowedWarehouseIds);
            return Ok(result);
        }

        /// <summary>
        /// Danh sách SKU hỗ trợ tìm kiếm / chọn nhanh cho bộ lọc biến động giá
        /// </summary>
        [HttpGet("price-volatility/skus")]
        public async Task<IActionResult> GetPriceVolatilitySkus()
        {
            if (!HasPermission("DASHBOARD_PRICE"))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền truy cập danh sách SKU Báo cáo Biến động Giá." });
            }

            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _dashboardService.GetPriceVolatilitySkusAsync(allowedWarehouseIds);
            return Ok(result);
        }
    }
}
