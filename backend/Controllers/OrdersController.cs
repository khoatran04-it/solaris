using backend.DTOs;
using backend.DTOs.OrderDTOs;
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
    /// API Quản lý Đơn Bán Hàng & Định Tuyến Kho (Sales Orders & Smart Routing Management).
    /// Trung tâm vận hành phân hệ Bán hàng B2C: Tạo đơn, xem danh sách phân trang có cách ly kho (Data Isolation),
    /// xem trước kết quả định tuyến kho (Smart Routing Preview), cập nhật trạng thái đơn, và hủy đơn hàng.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;
        private readonly IOrderRoutingService _routingService;

        public OrdersController(IOrderService service, IOrderRoutingService routingService)
        {
            _service = service;
            _routingService = routingService;
        }

        /// <summary>
        /// Lấy danh sách Đơn bán hàng có phân trang và bộ lọc đa chiều (Admin / Quản trị kho).
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo Mã đơn, Tên người nhận, SĐT người nhận hoặc Tên khách hàng.</param>
        /// <param name="customerId">Lọc theo ID khách hàng cụ thể.</param>
        /// <param name="warehouseId">Lọc theo ID kho xuất hàng.</param>
        /// <param name="status">Lọc theo trạng thái đơn hàng (1: Pending, 2: Confirmed, 3: Processing, 4: Shipping, 5: Completed, 6: Cancelled).</param>
        /// <param name="paymentStatus">Lọc theo trạng thái thanh toán (1: Unpaid, 2: PartiallyPaid, 3: Paid, 4: Refunded).</param>
        /// <param name="startDate">Lọc theo ngày đặt hàng từ ngày.</param>
        /// <param name="endDate">Lọc theo ngày đặt hàng đến ngày.</param>
        /// <param name="pageIndex">Số thứ tự trang hiện tại (mặc định 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định 10).</param>
        /// <returns>Danh sách đơn hàng đã được làm phẳng kèm thông tin phân trang.</returns>
        /// <response code="200">Truy vấn danh sách đơn hàng thành công.</response>
        /// <response code="401">Người dùng chưa đăng nhập hoặc token không hợp lệ.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<OrderReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? customerId,
            [FromQuery] int? warehouseId,
            [FromQuery] int? status,
            [FromQuery] int? paymentStatus,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, customerId, warehouseId, status, paymentStatus, startDate, endDate, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Truy xuất thông tin chi tiết một Đơn bán hàng theo ID.
        /// </summary>
        /// <param name="id">ID định danh duy nhất của đơn hàng trong hệ thống.</param>
        /// <returns>Dữ liệu chi tiết đơn hàng bao gồm danh sách mặt hàng đặt mua và các lô hàng thực tế đã xuất kho.</returns>
        /// <response code="200">Tìm thấy và trả về thông tin chi tiết đơn hàng.</response>
        /// <response code="404">Không tìm thấy đơn hàng với ID được cung cấp.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(OrderReadDto), StatusCodes.Status200OK)]
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
        /// Khởi tạo Đơn bán hàng mới (Dành cho Admin tạo đơn trực tiếp tại quầy hoặc qua điện thoại).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới đơn hàng bao gồm khách hàng, địa chỉ giao hàng và danh sách sản phẩm.</param>
        /// <returns>ID của đơn hàng vừa được tạo thành công.</returns>
        /// <response code="201">Tạo mới đơn hàng và giữ chỗ tồn kho thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc không đủ số lượng tồn kho.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
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
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Tạo đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xem trước kết quả Định tuyến kho thông minh (Smart Routing Preview) trước khi bấm tạo đơn.
        /// </summary>
        /// <param name="dto">Thông tin địa chỉ nhận hàng và danh sách mặt hàng dự kiến đặt mua.</param>
        /// <returns>Thông tin kho tối ưu được thuật toán lựa chọn dựa trên khoảng cách GPS và mức tồn kho thực tế.</returns>
        /// <response code="200">Phân tích và gợi ý kho xuất hàng thành công.</response>
        /// <response code="400">Không tìm thấy kho khả dụng hoặc dữ liệu phân tích không hợp lệ.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpPost("routing-preview")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> PreviewRouting([FromBody] OrderCreateDto dto)
        {
            try
            {
                var routing = await _routingService.DetermineOptimalWarehouseAsync(dto.CustomerAddressId, dto.Details);
                return Ok(routing);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái Đơn hàng, Trạng thái thanh toán hoặc Kho điều phối (Partial Update).
        /// </summary>
        /// <param name="id">ID của đơn hàng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu các trường cần thay đổi (chỉ ghi đè các trường có giá trị khác null).</param>
        /// <returns>Thông báo kết quả cập nhật trạng thái.</returns>
        /// <response code="200">Cập nhật trạng thái đơn hàng thành công.</response>
        /// <response code="400">Dữ liệu cập nhật không hợp lệ.</response>
        /// <response code="404">Không tìm thấy đơn hàng cần cập nhật.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpPut("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderUpdateDto dto)
        {
            try
            {
                await _service.UpdateStatusAsync(id, dto);
                return Ok(new { message = "Cập nhật trạng thái đơn hàng thành công" });
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
        /// Hủy đơn hàng và tự động giải phóng (Unreserve) số lượng tồn kho đã giữ chỗ.
        /// </summary>
        /// <param name="id">ID của đơn hàng cần hủy.</param>
        /// <param name="request">Đối tượng chứa lý do hủy đơn hàng.</param>
        /// <returns>Thông báo kết quả hủy đơn hàng.</returns>
        /// <response code="200">Hủy đơn hàng và hoàn trả tồn kho thành công.</response>
        /// <response code="400">Đơn hàng ở trạng thái không thể hủy (ví dụ: đã xuất kho hoặc đã giao thành công).</response>
        /// <response code="404">Không tìm thấy đơn hàng cần hủy.</response>
        /// <response code="401">Người dùng chưa đăng nhập.</response>
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.CancelAsync(id, request.Reason, userId);
                return Ok(new { message = "Hủy đơn hàng thành công" });
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
        /// Xóa mềm một Đơn bán hàng khỏi hệ thống (Soft Delete).
        /// </summary>
        /// <param name="id">ID của đơn hàng cần xóa.</param>
        /// <returns>Thông báo kết quả xóa đơn hàng.</returns>
        /// <response code="200">Xóa mềm đơn hàng thành công.</response>
        /// <response code="400">Đơn hàng không ở trạng thái được phép xóa (chỉ cho phép xóa đơn Confirmed hoặc Cancelled).</response>
        /// <response code="404">Không tìm thấy đơn hàng cần xóa.</response>
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
                return Ok(new { message = "Xóa đơn hàng thành công" });
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
    /// DTO Yêu cầu hủy đơn hàng.
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>
        /// Lý do hủy đơn hàng (Bắt buộc để phục vụ báo cáo và đối soát vận hành).
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
