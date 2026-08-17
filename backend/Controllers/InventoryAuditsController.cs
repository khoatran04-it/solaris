using backend.DTOs.InventoryAuditDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Các Đợt Kiểm Kê Kho Hàng (Stocktake / Inventory Audits & Blind Count).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryAuditsController : ControllerBase
    {
        private readonly IInventoryAuditService _service;

        public InventoryAuditsController(IInventoryAuditService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] int? status,
            [FromQuery] int? auditType,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, warehouseId, status, auditType, startDate, endDate, pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InventoryAuditCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (int.TryParse(userIdStr, out int userId)) dto.AuditorId = userId;

                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/submit-count")]
        public async Task<IActionResult> SubmitCount(int id, [FromBody] InventoryAuditSubmitCountDto dto)
        {
            try
            {
                await _service.SubmitCountAsync(id, dto);
                return NoContent();
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

        [HttpPost("{id}/approve-and-reconcile")]
        public async Task<IActionResult> ApproveAndReconcile(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (!int.TryParse(userIdStr, out int userId)) userId = 1;

                var adjId = await _service.ApproveAndReconcileAsync(id, userId);
                return Ok(new { adjustmentId = adjId, message = "Kiểm kê đã hoàn tất và tự động tạo phiếu điều chỉnh cân bằng kho." });
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

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelAuditRequest request)
        {
            try
            {
                await _service.CancelAsync(id, request.Reason);
                return NoContent();
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

    public class CancelAuditRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
