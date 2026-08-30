using backend.DTOs;
using backend.DTOs.InventoryTransferDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Phiếu Điều Chuyển Hàng Liên Kho 2 Bước (Inventory Transfers: Dispatch -> Receive).
    /// </summary>
    [Route("api/inventory-transfers")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InventoryTransfersController : ControllerBase
    {
        private readonly IInventoryTransferService _service;

        public InventoryTransfersController(IInventoryTransferService service)
        {
            _service = service;
        }

        /// <summary>
        /// Helper trích xuất danh sách ID kho mà nhân viên có quyền truy cập từ JWT Token Claims.
        /// </summary>
        private List<int>? GetAllowedWarehouseIds()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "WarehouseIds");
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
            }

            return null;
        }

        /// <summary>
        /// Lấy danh sách phiếu điều chuyển liên kho có phân trang và bộ lọc chuyên sâu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã phiếu chuyển, mã đơn hàng hoặc ghi chú.</param>
        /// <param name="fromWarehouseId">Lọc theo kho nguồn (kho xuất).</param>
        /// <param name="toWarehouseId">Lọc theo kho đích (kho nhận).</param>
        /// <param name="status">Trạng thái phiếu chuyển (1: Draft, 2: InTransit, 3: Completed, 4: Cancelled).</param>
        /// <param name="startDate">Lọc từ ngày tạo phiếu.</param>
        /// <param name="endDate">Lọc đến ngày tạo phiếu.</param>
        /// <param name="pageIndex">Trang hiện tại (bắt đầu từ 1, mặc định: 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định: 10).</param>
        /// <returns>Danh sách phân trang phiếu điều chuyển.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<InventoryTransferReadDto>), StatusCodes.Status200OK)]
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
            var allowedWarehouses = GetAllowedWarehouseIds();
            var result = await _service.GetPagedAsync(search, fromWarehouseId, toWarehouseId, status, startDate, endDate, pageIndex, pageSize, allowedWarehouses);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết phiếu điều chuyển liên kho kèm danh sách các dòng hàng theo ID.
        /// </summary>
        /// <param name="id">ID phiếu điều chuyển.</param>
        /// <returns>Dữ liệu chi tiết phiếu điều chuyển.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chuyển hoặc không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryTransferReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var allowedWarehouses = GetAllowedWarehouseIds();
                var result = await _service.GetByIdAsync(id, allowedWarehouses);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lập phiếu điều chuyển liên kho mới ở trạng thái Nháp (Draft).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo phiếu điều chuyển và danh sách mặt hàng/lô hàng.</param>
        /// <returns>ID phiếu điều chuyển vừa tạo.</returns>
        /// <response code="201">Tạo phiếu điều chuyển thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] InventoryTransferCreateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
                {
                    dto.CreatedById = userId;
                }

                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Tạo phiếu điều chuyển thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Bước 1: Xuất hàng đi (Dispatch) - Trừ tồn kho tại kho nguồn và chuyển trạng thái sang Đang vận chuyển (InTransit).
        /// </summary>
        /// <param name="id">ID phiếu điều chuyển cần xuất phát.</param>
        /// <returns>Thông báo kết quả xuất phát.</returns>
        /// <response code="200">Xuất hàng đi thành công.</response>
        /// <response code="400">Phiếu không ở trạng thái Draft hoặc kho nguồn không đủ số lượng tồn khả dụng.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chuyển.</response>
        [HttpPost("{id}/dispatch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Dispatch(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.DispatchTransferAsync(id, userId);
                return Ok(new { message = "Xuất hàng đi thành công, hàng đang trong quá trình vận chuyển" });
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
        /// Bước 2: Nhận hàng tại đích (Receive) - Cộng tồn kho tại kho đích và chuyển trạng thái sang Hoàn tất (Completed).
        /// </summary>
        /// <param name="id">ID phiếu điều chuyển cần nhận hàng.</param>
        /// <returns>Thông báo kết quả nhận hàng.</returns>
        /// <response code="200">Nhận hàng tại đích thành công.</response>
        /// <response code="400">Phiếu không ở trạng thái InTransit.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chuyển.</response>
        [HttpPost("{id}/receive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Receive(int id)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.ReceiveTransferAsync(id, userId);
                return Ok(new { message = "Nhận hàng tại đích thành công, đã cộng tồn kho và chốt sổ phiếu chuyển" });
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
        /// Hủy phiếu điều chuyển kho (chỉ cho phép khi phiếu còn ở trạng thái Draft).
        /// </summary>
        /// <param name="id">ID phiếu điều chuyển cần hủy.</param>
        /// <param name="request">Lý do hủy phiếu điều chuyển.</param>
        /// <returns>Thông báo kết quả hủy.</returns>
        /// <response code="200">Hủy phiếu điều chuyển thành công.</response>
        /// <response code="400">Lý do trống hoặc phiếu không ở trạng thái Draft.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu điều chuyển.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelTransferRequest request)
        {
            try
            {
                await _service.CancelTransferAsync(id, request.Reason);
                return Ok(new { message = "Hủy phiếu điều chuyển thành công" });
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
    /// Payload yêu cầu hủy phiếu điều chuyển.
    /// </summary>
    public class CancelTransferRequest
    {
        /// <summary>Lý do hủy phiếu điều chuyển (bắt buộc).</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
