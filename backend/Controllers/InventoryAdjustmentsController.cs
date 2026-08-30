using backend.DTOs;
using backend.DTOs.InventoryAdjustmentDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Phiếu Điều Chỉnh, Xuất Hủy & Cân Bằng Tồn Kho (Inventory Adjustments & Write-Offs).
    /// </summary>
    [Route("api/inventory-adjustments")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InventoryAdjustmentsController : ControllerBase
    {
        private readonly IInventoryAdjustmentService _service;

        public InventoryAdjustmentsController(IInventoryAdjustmentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách phiếu điều chỉnh tồn kho phân trang kèm bộ lọc đa tiêu chí.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã phiếu điều chỉnh, mã đợt kiểm kê, tên người lập, ghi chú).</param>
        /// <param name="warehouseId">Lọc theo kho cụ thể.</param>
        /// <param name="status">Lọc theo trạng thái phiếu điều chỉnh (Draft, Approved, Cancelled).</param>
        /// <param name="reason">Lọc theo lý do điều chỉnh (Surplus, LossTheft, Spoilage, Damage, Expiry, Shrinkage, DataCorrection).</param>
        /// <param name="startDate">Lọc từ ngày.</param>
        /// <param name="endDate">Lọc đến ngày.</param>
        /// <param name="pageIndex">Trang hiện tại (Mặc định: 1).</param>
        /// <param name="pageSize">Kích thước trang (Mặc định: 10).</param>
        /// <returns>Danh sách phiếu điều chỉnh phân trang.</returns>
        /// <response code="200">Lấy danh sách thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<InventoryAdjustmentReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] int? status,
            [FromQuery] int? reason,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, warehouseId, status, reason, startDate, endDate, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết phiếu điều chỉnh tồn kho theo ID kèm danh sách dòng hàng hóa biến động.
        /// </summary>
        /// <param name="id">ID phiếu điều chỉnh.</param>
        /// <returns>Dữ liệu chi tiết phiếu điều chỉnh.</returns>
        /// <response code="200">Lấy chi tiết thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chỉnh.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryAdjustmentReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Tạo mới phiếu đề xuất điều chỉnh tồn kho ở trạng thái Nháp (Draft).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo phiếu điều chỉnh.</param>
        /// <returns>ID phiếu điều chỉnh vừa tạo.</returns>
        /// <response code="201">Tạo phiếu điều chỉnh thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Kho hàng không tồn tại.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] InventoryAdjustmentCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    dto.CreatedById = userId;
                }

                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Tạo phiếu điều chỉnh tồn kho thành công" });
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

        /// <summary>
        /// Phê duyệt phiếu điều chỉnh: Cập nhật trực tiếp số dư tồn kho và ghi nhật ký sổ cái (InventoryTransaction).
        /// </summary>
        /// <param name="id">ID phiếu điều chỉnh cần phê duyệt.</param>
        /// <returns>Kết quả phê duyệt.</returns>
        /// <response code="200">Duyệt phiếu điều chỉnh thành công.</response>
        /// <response code="400">Trạng thái phiếu không hợp lệ (Không phải Draft).</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chỉnh.</response>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.ApproveAdjustmentAsync(id, userId);
                return Ok(new { message = "Duyệt phiếu điều chỉnh tồn kho thành công, đã cập nhật số dư tồn kho và ghi sổ cái" });
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

        /// <summary>
        /// Hủy phiếu đề xuất điều chỉnh tồn kho kèm lý do bắt buộc.
        /// </summary>
        /// <param name="id">ID phiếu điều chỉnh cần hủy.</param>
        /// <param name="request">Lý do hủy phiếu điều chỉnh.</param>
        /// <returns>Kết quả hủy.</returns>
        /// <response code="200">Hủy phiếu điều chỉnh thành công.</response>
        /// <response code="400">Phiếu đã duyệt không thể hủy.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chỉnh.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelAdjustmentRequest request)
        {
            try
            {
                await _service.CancelAsync(id, request.Reason);
                return Ok(new { message = "Hủy phiếu điều chỉnh tồn kho thành công" });
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

        /// <summary>
        /// Xóa mềm phiếu đề xuất điều chỉnh tồn kho (Chỉ áp dụng cho phiếu chưa duyệt).
        /// </summary>
        /// <param name="id">ID phiếu điều chỉnh cần xóa.</param>
        /// <returns>Kết quả xóa.</returns>
        /// <response code="200">Xóa phiếu điều chỉnh thành công.</response>
        /// <response code="400">Phiếu đã duyệt không thể xóa.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chỉnh.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa phiếu điều chỉnh tồn kho thành công" });
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

    /// <summary>
    /// Payload yêu cầu hủy phiếu điều chỉnh tồn kho.
    /// </summary>
    public class CancelAdjustmentRequest
    {
        /// <summary>Lý do hủy phiếu điều chỉnh.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
