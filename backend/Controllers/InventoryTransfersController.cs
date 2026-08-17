using backend.DTOs.InventoryTransferDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Phiếu Điều Chuyển Hàng Liên Kho 2 Bước (Inventory Transfers).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryTransfersController : ControllerBase
    {
        private readonly IInventoryTransferService _service;

        public InventoryTransfersController(IInventoryTransferService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? fromWarehouseId,
            [FromQuery] int? toWarehouseId,
            [FromQuery] int? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, fromWarehouseId, toWarehouseId, status, startDate, endDate, pageIndex, pageSize);
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
        public async Task<IActionResult> Create([FromBody] InventoryTransferCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (int.TryParse(userIdStr, out int userId)) dto.CreatedById = userId;

                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/dispatch")]
        public async Task<IActionResult> Dispatch(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (!int.TryParse(userIdStr, out int userId)) userId = 1;

                await _service.DispatchTransferAsync(id, userId);
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

        [HttpPost("{id}/receive")]
        public async Task<IActionResult> Receive(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1";
                if (!int.TryParse(userIdStr, out int userId)) userId = 1;

                await _service.ReceiveTransferAsync(id, userId);
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
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelTransferRequest request)
        {
            try
            {
                await _service.CancelTransferAsync(id, request.Reason);
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

    public class CancelTransferRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
