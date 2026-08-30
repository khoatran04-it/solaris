using backend.DTOs;
using backend.DTOs.InventoryIssueDTOs;
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
    /// API Quản lý Phiếu Xuất Kho & Gợi Ý Lấy Hàng Theo Lô FEFO (Goods Issue Note).
    /// </summary>
    [Route("api/inventory-issues")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InventoryIssuesController : ControllerBase
    {
        private readonly IInventoryIssueService _service;

        public InventoryIssuesController(IInventoryIssueService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách phiếu xuất kho có phân trang và bộ lọc chuyên sâu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã phiếu xuất, tên người nhận, mã đơn hàng hoặc ghi chú.</param>
        /// <param name="warehouseId">ID kho hàng thực hiện xuất.</param>
        /// <param name="status">Trạng thái phiếu xuất (1: Pending, 2: Picking, 3: Completed, 4: Cancelled).</param>
        /// <param name="startDate">Lọc từ ngày xuất hàng.</param>
        /// <param name="endDate">Lọc đến ngày xuất hàng.</param>
        /// <param name="pageIndex">Trang hiện tại (bắt đầu từ 1, mặc định: 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định: 10).</param>
        /// <returns>Danh sách phân trang phiếu xuất kho.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<InventoryIssueReadDto>), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Lấy chi tiết thông tin phiếu xuất kho và danh sách các dòng mặt hàng kèm lô xuất theo ID.
        /// </summary>
        /// <param name="id">ID phiếu xuất kho.</param>
        /// <returns>Dữ liệu chi tiết phiếu xuất kho.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu xuất kho.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryIssueReadDto), StatusCodes.Status200OK)]
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
        /// Gợi ý danh sách các lô hàng tối ưu cần nhặt theo thuật toán FEFO (First-Expired, First-Out).
        /// </summary>
        /// <param name="warehouseId">ID kho hàng xuất.</param>
        /// <param name="variantId">ID biến thể sản phẩm cần xuất.</param>
        /// <param name="neededQuantity">Tổng số lượng cần xuất.</param>
        /// <returns>Danh sách các lô hàng và số lượng đề xuất nhặt từ mỗi lô.</returns>
        /// <response code="200">Gợi ý thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet("suggested-batches")]
        [ProducesResponseType(typeof(List<SuggestedBatchDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSuggestedBatches(
            [FromQuery] int warehouseId,
            [FromQuery] int variantId,
            [FromQuery] decimal neededQuantity)
        {
            var result = await _service.GetSuggestedBatchesAsync(warehouseId, variantId, neededQuantity);
            return Ok(result);
        }

        /// <summary>
        /// Khởi tạo phiếu xuất kho mới ở trạng thái chờ xử lý (Pending).
        /// </summary>
        /// <param name="dto">Dữ liệu phiếu xuất kho và danh sách mặt hàng / lô hàng.</param>
        /// <returns>ID phiếu xuất kho vừa tạo.</returns>
        /// <response code="201">Tạo phiếu xuất kho thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] InventoryIssueCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                var newId = await _service.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Tạo phiếu xuất kho thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hoàn tất phiếu xuất kho: Trừ số lượng tồn kho (ưu tiên trừ hàng giữ chỗ Reserved), ghi sổ cái giao dịch và cập nhật đơn hàng.
        /// </summary>
        /// <param name="id">ID phiếu xuất kho cần hoàn tất.</param>
        /// <param name="request">Ghi chú bổ sung khi hoàn tất.</param>
        /// <returns>Thông báo kết quả xử lý.</returns>
        /// <response code="200">Hoàn tất xuất kho thành công.</response>
        /// <response code="400">Phiếu xuất kho không ở trạng thái hợp lệ để chốt sổ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu xuất kho.</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete(int id, [FromBody] CompleteIssueRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.CompleteIssueAsync(id, userId, request.Note);
                return Ok(new { message = "Hoàn tất xuất kho thành công" });
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
        /// Hủy phiếu xuất kho chưa hoàn tất với lý do cụ thể.
        /// </summary>
        /// <param name="id">ID phiếu xuất kho cần hủy.</param>
        /// <param name="request">Lý do hủy phiếu xuất.</param>
        /// <returns>Thông báo kết quả hủy.</returns>
        /// <response code="200">Hủy phiếu xuất kho thành công.</response>
        /// <response code="400">Lý do trống hoặc phiếu đã chốt sổ không thể hủy.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu xuất kho.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelIssueRequest request)
        {
            try
            {
                await _service.CancelIssueAsync(id, request.Reason);
                return Ok(new { message = "Hủy phiếu xuất kho thành công" });
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
        /// Xóa mềm một phiếu xuất kho (chỉ áp dụng cho phiếu chưa chốt sổ).
        /// </summary>
        /// <param name="id">ID phiếu xuất kho cần xóa.</param>
        /// <returns>Thông báo kết quả xóa.</returns>
        /// <response code="200">Xóa phiếu xuất kho thành công.</response>
        /// <response code="400">Phiếu xuất kho đã hoàn tất không thể xóa.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu xuất kho.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa phiếu xuất kho thành công" });
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
    /// Payload yêu cầu hoàn tất phiếu xuất kho.
    /// </summary>
    public class CompleteIssueRequest
    {
        /// <summary>Ghi chú bổ sung khi hoàn tất.</summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Payload yêu cầu hủy phiếu xuất kho.
    /// </summary>
    public class CancelIssueRequest
    {
        /// <summary>Lý do hủy phiếu xuất kho (bắt buộc).</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
