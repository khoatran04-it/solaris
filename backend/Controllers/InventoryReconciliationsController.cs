using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Báo Cáo Chốt Ca, Sổ Cái Tồn Kho & Đối Soát Số Liệu (Shift Closing & Stock Reconciliation).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryReconciliationsController : ControllerBase
    {
        private readonly IInventoryReconciliationService _service;

        public InventoryReconciliationsController(IInventoryReconciliationService service)
        {
            _service = service;
        }

        [HttpGet("shift-closing")]
        public async Task<IActionResult> GetShiftClosing(
            [FromQuery] int warehouseId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            try
            {
                var from = fromDate ?? DateTime.UtcNow.Date;
                var to = toDate ?? DateTime.UtcNow.Date;

                var result = await _service.GetShiftClosingReportAsync(warehouseId, from, to);
                return Ok(result);
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

        [HttpGet("stock-ledger")]
        public async Task<IActionResult> GetStockLedger(
            [FromQuery] int warehouseId,
            [FromQuery] int? variantId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 15)
        {
            try
            {
                var result = await _service.GetStockLedgerAsync(warehouseId, variantId, fromDate, toDate, pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
