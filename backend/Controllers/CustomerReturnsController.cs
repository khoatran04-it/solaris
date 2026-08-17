using backend.DTOs.CustomerReturnDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerReturnsController : ControllerBase
    {
        private readonly ICustomerReturnService _service;

        public CustomerReturnsController(ICustomerReturnService service)
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerReturnCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (int.TryParse(userIdStr, out int userId)) dto.ReceivedById = userId;

                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/inspect-and-complete")]
        public async Task<IActionResult> InspectAndComplete(int id, [FromBody] CustomerReturnInspectionDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (!int.TryParse(userIdStr, out int userId)) userId = 1;

                await _service.InspectAndCompleteAsync(id, userId, dto);
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

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectReturnRequest request)
        {
            try
            {
                await _service.RejectReturnAsync(id, request.Reason);
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

    public class RejectReturnRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
