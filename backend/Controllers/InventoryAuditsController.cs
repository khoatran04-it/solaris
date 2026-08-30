using backend.DTOs;
using backend.DTOs.InventoryAuditDTOs;
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
    /// API Quản lý Các Đợt Kiểm Kê Kho Hàng (Stocktake / Inventory Audits & Blind Count).
    /// </summary>
    [Route("api/inventory-audits")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InventoryAuditsController : ControllerBase
    {
        private readonly IInventoryAuditService _service;

        public InventoryAuditsController(IInventoryAuditService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách đợt kiểm kê phân trang kèm bộ lọc đa tiêu chí.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm (Mã đợt kiểm kê, tên nhân viên kiểm kê, ghi chú).</param>
        /// <param name="warehouseId">Lọc theo kho cụ thể.</param>
        /// <param name="status">Lọc theo trạng thái kiểm kê.</param>
        /// <param name="auditType">Lọc theo loại hình kiểm kê (Full, Cycle, Spot).</param>
        /// <param name="startDate">Lọc từ ngày.</param>
        /// <param name="endDate">Lọc đến ngày.</param>
        /// <param name="pageIndex">Trang hiện tại (Mặc định: 1).</param>
        /// <param name="pageSize">Kích thước trang (Mặc định: 10).</param>
        /// <returns>Danh sách đợt kiểm kê phân trang.</returns>
        /// <response code="200">Lấy danh sách thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<InventoryAuditReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        /// <summary>
        /// Lấy chi tiết đợt kiểm kê theo ID kèm toàn bộ danh sách dòng kiểm đếm đối soát.
        /// </summary>
        /// <param name="id">ID đợt kiểm kê.</param>
        /// <returns>Dữ liệu chi tiết đợt kiểm kê.</returns>
        /// <response code="200">Lấy chi tiết thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy đợt kiểm kê.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryAuditReadDto), StatusCodes.Status200OK)]
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
        /// Khởi tạo đợt kiểm kê mới (Tự động chụp ảnh số dư hệ thống Snapshot tại thời điểm tạo).
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo đợt kiểm kê.</param>
        /// <returns>ID đợt kiểm kê vừa tạo.</returns>
        /// <response code="201">Tạo đợt kiểm kê thành công.</response>
        /// <response code="400">Dữ liệu không hợp lệ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Kho hàng không tồn tại.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] InventoryAuditCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    dto.AuditorId = userId;
                }

                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Khởi tạo đợt kiểm kê kho thành công" });
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
        /// Nộp kết quả kiểm đếm thực tế (Blind Count Submit) và tự động tính toán chênh lệch.
        /// </summary>
        /// <param name="id">ID đợt kiểm kê.</param>
        /// <param name="dto">Danh sách số lượng thực tế kiểm đếm được.</param>
        /// <returns>Kết quả cập nhật.</returns>
        /// <response code="200">Nộp số liệu kiểm đếm thành công.</response>
        /// <response code="400">Trạng thái phiếu không hợp lệ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy đợt kiểm kê.</response>
        [HttpPost("{id}/submit-count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitCount(int id, [FromBody] InventoryAuditSubmitCountDto dto)
        {
            try
            {
                await _service.SubmitCountAsync(id, dto);
                return Ok(new { message = "Nộp kết quả kiểm đếm thực tế thành công, đã chuyển sang trạng thái Chờ duyệt" });
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
        /// Phê duyệt chốt kiểm kê và tự động sinh phiếu điều chỉnh (InventoryAdjustment) để cân bằng sổ cái.
        /// </summary>
        /// <param name="id">ID đợt kiểm kê.</param>
        /// <returns>ID phiếu điều chỉnh được tự động sinh ra.</returns>
        /// <response code="200">Chốt kiểm kê và tạo phiếu điều chỉnh thành công.</response>
        /// <response code="400">Trạng thái phiếu không hợp lệ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy đợt kiểm kê.</response>
        [HttpPost("{id}/approve-and-reconcile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveAndReconcile(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                var adjId = await _service.ApproveAndReconcileAsync(id, userId);
                return Ok(new
                {
                    adjustmentId = adjId,
                    message = "Kiểm kê đã hoàn tất và tự động tạo phiếu điều chỉnh cân bằng kho"
                });
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
        /// Hủy đợt kiểm kê kèm lý do bắt buộc.
        /// </summary>
        /// <param name="id">ID đợt kiểm kê.</param>
        /// <param name="request">Lý do hủy đợt kiểm kê.</param>
        /// <returns>Kết quả hủy.</returns>
        /// <response code="200">Hủy đợt kiểm kê thành công.</response>
        /// <response code="400">Phiếu đã hoàn tất không thể hủy.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy đợt kiểm kê.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelAuditRequest request)
        {
            try
            {
                await _service.CancelAsync(id, request.Reason);
                return Ok(new { message = "Hủy đợt kiểm kê thành công" });
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
    /// Payload yêu cầu hủy đợt kiểm kê.
    /// </summary>
    public class CancelAuditRequest
    {
        /// <summary>Lý do hủy đợt kiểm kê.</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
