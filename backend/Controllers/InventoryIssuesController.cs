using backend.DTOs.InventoryIssueDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryIssuesController : ControllerBase
    {
        private readonly IInventoryIssueService _service;

        public InventoryIssuesController(IInventoryIssueService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] int? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, warehouseId, status, startDate, endDate, pageIndex, pageSize);
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

        [HttpGet("suggested-batches")]
        public async Task<IActionResult> GetSuggestedBatches(
            [FromQuery] int warehouseId,
            [FromQuery] int variantId,
            [FromQuery] decimal neededQuantity)
        {
            var result = await _service.GetSuggestedBatchesAsync(warehouseId, variantId, neededQuantity);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InventoryIssueCreateDto dto)
        {
            try
            {
                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> Complete(int id, [FromBody] CompleteIssueRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (!int.TryParse(userIdStr, out int userId)) userId = 1;

                await _service.CompleteIssueAsync(id, userId, request.Note);
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

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelIssueRequest request)
        {
            try
            {
                await _service.CancelIssueAsync(id, request.Reason);
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

    public class CompleteIssueRequest
    {
        public string? Note { get; set; }
    }

    public class CancelIssueRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
