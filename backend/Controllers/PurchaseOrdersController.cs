using backend.DTOs;
using backend.DTOs.PurchaseOrderDTOs;
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
    /// API Quản lý Đơn Đặt Mua Hàng Từ Nhà Cung Cấp (Purchase Orders - PO).
    /// </summary>
    [ApiController]
    [Route("api/purchase-orders")]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _service;

        public PurchaseOrdersController(IPurchaseOrderService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách đơn mua hàng không phân trang (dùng cho dropdown/chọn PO khi lập phiếu nhập kho).
        /// </summary>
        /// <response code="200">Trả về danh sách đơn mua hàng hợp lệ.</response>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<PurchaseOrderReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _service.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đơn đặt mua hàng có phân trang, tìm kiếm và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã PO, tên nhà cung cấp hoặc ghi chú.</param>
        /// <param name="supplierId">Lọc theo ID nhà cung cấp.</param>
        /// <param name="status">Lọc theo trạng thái đơn hàng (1: Draft, 2: Processing, 3: Approved, 4: PartiallyReceived, 5: Completed, 6: Cancelled).</param>
        /// <param name="startDate">Lọc theo ngày lập đơn từ ngày.</param>
        /// <param name="endDate">Lọc theo ngày lập đơn đến ngày.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (mặc định 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên một trang (mặc định 10).</param>
        /// <response code="200">Trả về dữ liệu phân trang đơn mua hàng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PurchaseOrderReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? supplierId,
            [FromQuery] int? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, supplierId, status, startDate, endDate, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một đơn mua hàng theo ID kèm danh sách sản phẩm.
        /// </summary>
        /// <param name="id">Mã định danh đơn mua hàng.</param>
        /// <response code="200">Trả về chi tiết đơn mua hàng.</response>
        /// <response code="404">Không tìm thấy đơn mua hàng với ID chỉ định.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PurchaseOrderReadDto), StatusCodes.Status200OK)]
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
        /// Tạo mới một đơn đặt mua hàng ở trạng thái Nháp (Draft).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới đơn mua hàng.</param>
        /// <response code="201">Tạo mới thành công, trả về thông tin chi tiết đơn vừa tạo.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(PurchaseOrderReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] PurchaseOrderCreateDto dto)
        {
            try
            {
                // Trích xuất ID nhân viên lập đơn từ JWT Token Claims
                int currentUserId = 0;
                var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimVal, out int parsedId))
                {
                    currentUserId = parsedId;
                }

                var newId = await _service.CreateAsync(dto, currentUserId);
                var created = await _service.GetByIdAsync(newId);
                return CreatedAtAction(nameof(GetById), new { id = newId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Chỉnh sửa toàn bộ thông tin đơn mua hàng và danh sách sản phẩm (Chỉ áp dụng khi đơn ở trạng thái Nháp).
        /// </summary>
        /// <param name="id">Mã định danh đơn mua hàng cần sửa.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        /// <response code="200">Cập nhật thành công.</response>
        /// <response code="404">Không tìm thấy đơn mua hàng.</response>
        /// <response code="400">Đơn hàng không ở trạng thái Nháp hoặc dữ liệu không hợp lệ.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] PurchaseOrderCreateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật đơn đặt mua hàng thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái vòng đời hoặc thông tin vận hành của đơn mua hàng (Workflow: Gửi duyệt, Duyệt, Hủy đơn...).
        /// </summary>
        /// <param name="id">Mã định danh đơn mua hàng.</param>
        /// <param name="dto">Thông tin trạng thái mới và lý do hủy (nếu có).</param>
        /// <response code="200">Cập nhật trạng thái thành công.</response>
        /// <response code="404">Không tìm thấy đơn mua hàng.</response>
        /// <response code="400">Vi phạm quy tắc chuyển đổi trạng thái hoặc thiếu lý do hủy đơn.</response>
        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] PurchaseOrderStatusUpdateDto dto)
        {
            try
            {
                await _service.UpdateStatusAsync(id, dto);
                return Ok(new { message = "Cập nhật trạng thái đơn mua hàng thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Chốt đóng đơn mua hàng sớm theo số lượng thực nhận (Settle & Close PO).
        /// Áp dụng khi đơn ở trạng thái PartiallyReceived và NCC không giao tiếp phần hàng thiếu/hỏng.
        /// </summary>
        /// <param name="id">Mã định danh đơn mua hàng cần chốt đóng.</param>
        /// <param name="dto">Dữ liệu lý do chốt đóng đơn.</param>
        [HttpPost("{id}/close-and-settle")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloseAndSettle(int id, [FromBody] ClosePurchaseOrderDto dto)
        {
            try
            {
                int currentUserId = 0;
                var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimVal, out int parsedId))
                {
                    currentUserId = parsedId;
                }

                await _service.CloseAndSettleOrderAsync(id, dto.Reason, currentUserId);
                return Ok(new { message = "Chốt đóng đơn mua hàng và quyết toán công nợ thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa mềm đơn đặt mua hàng (Chỉ cho phép khi đơn ở trạng thái Nháp hoặc Đã hủy và chưa có phiếu nhập kho).
        /// </summary>
        /// <param name="id">Mã định danh đơn mua hàng cần xóa.</param>
        /// <response code="200">Xóa thành công.</response>
        /// <response code="404">Không tìm thấy đơn mua hàng.</response>
        /// <response code="400">Đơn hàng không thỏa mãn điều kiện xóa an toàn.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa đơn đặt mua hàng thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
