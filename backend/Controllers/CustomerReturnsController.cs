using backend.DTOs;
using backend.DTOs.CustomerReturnDTOs;
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
    /// API Quản lý Phiếu Khách Hàng Trả Hàng & Nghiệm Thu QC (Customer Returns & RMA Management).
    /// Trung tâm vận hành phân hệ Thu hồi & Hoàn tiền: Tiếp nhận yêu cầu trả hàng (RMA), phân quyền kho (Data Isolation),
    /// kiểm định chất lượng (QC inspection), hoàn tất nhập kho và tự động hạch toán tiền hoàn trả.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/customer-returns")]
    [Produces("application/json")]
    [Authorize]
    public class CustomerReturnsController : ControllerBase
    {
        private readonly ICustomerReturnService _service;

        public CustomerReturnsController(ICustomerReturnService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy danh sách Phiếu trả hàng có phân trang và bộ lọc nâng cao (Kế toán / Thủ kho).
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Mã phiếu trả, Mã đơn hàng, Tên khách hàng hoặc Lý do trả.</param>
        /// <param name="warehouseId">Lọc theo ID kho tiếp nhận hàng hoàn trả.</param>
        /// <param name="status">Lọc theo trạng thái phiếu trả (1: Pending, 2: Inspecting, 3: Completed, 4: Rejected).</param>
        /// <param name="startDate">Lọc theo ngày tạo phiếu trả từ ngày.</param>
        /// <param name="endDate">Lọc theo ngày tạo phiếu trả đến ngày.</param>
        /// <param name="pageIndex">Số thứ tự trang hiện tại (mặc định 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định 10).</param>
        /// <returns>Danh sách phiếu trả hàng đã được làm phẳng kèm thông tin phân trang.</returns>
        /// <response code="200">Truy vấn danh sách phiếu trả hàng thành công.</response>
        /// <response code="401">Người dùng chưa đăng nhập hoặc token không hợp lệ.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerReturnReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// Truy xuất thông tin chi tiết một Phiếu trả hàng theo ID.
        /// </summary>
        /// <param name="id">ID định danh duy nhất của phiếu trả hàng trong hệ thống.</param>
        /// <returns>Dữ liệu chi tiết phiếu trả hàng bao gồm thông tin đơn gốc, danh sách mặt hàng, số lượng đạt/hỏng và tiền hoàn.</returns>
        /// <response code="200">Tìm thấy và trả về thông tin chi tiết phiếu trả hàng.</response>
        /// <response code="404">Không tìm thấy phiếu trả hàng với ID được cung cấp.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerReturnReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
        /// Khởi tạo Phiếu trả hàng mới (Tiếp nhận yêu cầu RMA ban đầu ở trạng thái Pending).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo phiếu trả hàng bao gồm đơn hàng gốc, kho tiếp nhận và danh sách mặt hàng hoàn trả.</param>
        /// <returns>ID của phiếu trả hàng vừa được tạo thành công.</returns>
        /// <response code="201">Tạo mới phiếu trả hàng thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc đơn hàng không tồn tại.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CustomerReturnCreateDto dto)
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
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Tạo phiếu trả hàng thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Duyệt Phiếu Yêu Cầu Trả Hàng (Pending -> Approved).
        /// </summary>
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

                await _service.ApproveAsync(id, userId);
                return Ok(new { message = "Đã duyệt yêu cầu trả hàng thành công" });
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
        /// BƯỚC 2: Kiểm định chất lượng tại kho (Approved -> Inspecting).
        /// </summary>
        [HttpPost("{id:int}/inspect")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Inspect(int id, [FromBody] CustomerReturnInspectionDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.InspectQCAsync(id, userId, dto);
                return Ok(new { message = "Nghiệm thu kiểm định QC thành công" });
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
        /// BƯỚC 3: Hoàn tất trực tiếp Phiếu trả hàng (Inspecting -> Completed).
        /// </summary>
        [HttpPost("{id:int}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                await _service.CompleteReturnAsync(id);
                return Ok(new { message = "Hoàn tất phiếu trả hàng thành công" });
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
        /// Kiểm định chất lượng (QC Inspection) và Hoàn tất phiếu trả hàng (Nhập kho + Hoàn tiền).
        /// </summary>
        [HttpPost("{id:int}/inspect-and-complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> InspectAndComplete(int id, [FromBody] CustomerReturnInspectionDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.InspectAndCompleteAsync(id, userId, dto);
                return Ok(new { message = "Nghiệm thu phiếu trả hàng thành công" });
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
        /// Từ chối Phiếu yêu cầu trả hàng (Đóng luồng xử lý mà không tác động tới tồn kho).
        /// </summary>
        /// <param name="id">ID của phiếu trả hàng cần từ chối.</param>
        /// <param name="request">Đối tượng chứa lý do từ chối tiếp nhận đổi trả.</param>
        /// <returns>Thông báo kết quả từ chối phiếu trả hàng.</returns>
        /// <response code="200">Từ chối phiếu trả hàng thành công.</response>
        /// <response code="400">Phiếu trả hàng đã hoàn tất nên không thể từ chối.</response>
        /// <response code="404">Không tìm thấy phiếu trả hàng cần từ chối.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectReturnRequest request)
        {
            try
            {
                await _service.RejectReturnAsync(id, request.Reason);
                return Ok(new { message = "Từ chối phiếu trả hàng thành công" });
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
        /// Xóa mềm một Phiếu trả hàng khỏi hệ thống (Soft Delete).
        /// </summary>
        /// <param name="id">ID của phiếu trả hàng cần xóa.</param>
        /// <returns>Thông báo kết quả xóa phiếu trả hàng.</returns>
        /// <response code="200">Xóa mềm phiếu trả hàng thành công.</response>
        /// <response code="400">Phiếu trả hàng đã hoàn tất nên không được phép xóa (bảo toàn sổ cái kế toán).</response>
        /// <response code="404">Không tìm thấy phiếu trả hàng cần xóa.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa phiếu trả hàng thành công" });
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
    /// DTO Yêu cầu từ chối phiếu trả hàng.
    /// </summary>
    public class RejectReturnRequest
    {
        /// <summary>
        /// Lý do từ chối nhận hàng (Bắt buộc để phản hồi minh bạch cho khách hàng).
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
