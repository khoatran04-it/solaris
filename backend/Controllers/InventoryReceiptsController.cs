using backend.DTOs;
using backend.DTOs.InventoryReceiptDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Phiếu Nhập Kho & Kiểm Đếm Chất Lượng Nông Sản (Goods Receipt Note - GRN).
    /// </summary>
    [Route("api/inventory-receipts")]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InventoryReceiptsController : ControllerBase
    {
        private readonly IInventoryReceiptService _service;

        public InventoryReceiptsController(IInventoryReceiptService service)
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

            return null; // Mặc định nếu không có claim giới hạn thì xem toàn cục (SuperAdmin / HQ)
        }

        /// <summary>
        /// Lấy danh sách phiếu nhập kho có phân trang và bộ lọc đa chiều.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã phiếu, tên nhà cung cấp hoặc ghi chú.</param>
        /// <param name="warehouseId">ID kho hàng tiếp nhận.</param>
        /// <param name="supplierId">ID nhà cung cấp giao hàng.</param>
        /// <param name="status">Trạng thái phiếu nhập (1: Pending, 2: Inspecting, 3: Completed, 4: Cancelled).</param>
        /// <param name="startDate">Lọc từ ngày lập phiếu.</param>
        /// <param name="endDate">Lọc đến ngày lập phiếu.</param>
        /// <param name="pageIndex">Trang hiện tại (bắt đầu từ 1, mặc định: 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định: 10).</param>
        /// <returns>Danh sách phân trang phiếu nhập kho.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<InventoryReceiptReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] int? supplierId,
            [FromQuery] int? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var allowedWarehouseIds = GetAllowedWarehouseIds();
            var result = await _service.GetPagedAsync(search, warehouseId, supplierId, status, startDate, endDate, pageIndex, pageSize, allowedWarehouseIds);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một phiếu nhập kho kèm danh sách kiểm đếm hàng hóa theo ID.
        /// </summary>
        /// <param name="id">ID phiếu nhập kho.</param>
        /// <returns>Dữ liệu chi tiết phiếu nhập kho.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu nhập kho.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryReceiptReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var allowedWarehouseIds = GetAllowedWarehouseIds();
                var result = await _service.GetByIdAsync(id, allowedWarehouseIds);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Khởi tạo phiếu nhập kho mới ở trạng thái chờ kiểm đếm (Pending).
        /// </summary>
        /// <param name="dto">Dữ liệu tạo phiếu nhập kho và danh sách hàng hóa.</param>
        /// <returns>Mã ID phiếu nhập kho vừa tạo.</returns>
        /// <response code="201">Tạo phiếu nhập kho thành công.</response>
        /// <response code="400">Dữ liệu đầu vào không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] InventoryReceiptCreateDto dto)
        {
            try
            {
                var newId = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId, message = "Tạo phiếu nhập kho thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Hoàn tất phiếu nhập kho: Chốt sổ kiểm đếm, chính thức cộng số lượng vào tồn kho khả dụng và ghi sổ cái giao dịch.
        /// </summary>
        /// <param name="id">ID phiếu nhập kho cần hoàn tất.</param>
        /// <param name="request">Ghi chú bổ sung khi hoàn tất.</param>
        /// <returns>Thông báo kết quả xử lý.</returns>
        /// <response code="200">Hoàn tất nhập kho thành công.</response>
        /// <response code="400">Phiếu nhập kho không ở trạng thái hợp lệ để chốt sổ.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu nhập kho.</response>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete(int id, [FromBody] CompleteReceiptRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int userId = 1;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int parsedId))
                {
                    userId = parsedId;
                }

                await _service.CompleteReceiptAsync(id, userId, request.Note);
                return Ok(new { message = "Hoàn tất nhập kho thành công, đã cộng tồn khả dụng và ghi sổ cái" });
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
        /// Hủy phiếu nhập kho chưa hoàn tất với lý do cụ thể.
        /// </summary>
        /// <param name="id">ID phiếu nhập kho cần hủy.</param>
        /// <param name="request">Lý do hủy phiếu nhập.</param>
        /// <returns>Thông báo kết quả hủy.</returns>
        /// <response code="200">Hủy phiếu nhập kho thành công.</response>
        /// <response code="400">Lý do trống hoặc phiếu đã chốt sổ không thể hủy.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu nhập kho.</response>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelReceiptRequest request)
        {
            try
            {
                await _service.CancelReceiptAsync(id, request.Reason);
                return Ok(new { message = "Hủy phiếu nhập kho thành công" });
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
        /// Xóa mềm một phiếu nhập kho (chỉ áp dụng cho phiếu chưa chốt sổ).
        /// </summary>
        /// <param name="id">ID phiếu nhập kho cần xóa.</param>
        /// <returns>Thông báo kết quả xóa.</returns>
        /// <response code="200">Xóa phiếu nhập kho thành công.</response>
        /// <response code="400">Phiếu nhập kho đã hoàn tất không thể xóa.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy phiếu nhập kho.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa phiếu nhập kho thành công" });
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
    /// Payload yêu cầu hoàn tất phiếu nhập kho.
    /// </summary>
    public class CompleteReceiptRequest
    {
        /// <summary>Ghi chú bổ sung khi hoàn tất.</summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Payload yêu cầu hủy phiếu nhập kho.
    /// </summary>
    public class CancelReceiptRequest
    {
        /// <summary>Lý do hủy phiếu nhập kho (bắt buộc).</summary>
        public string Reason { get; set; } = string.Empty;
    }
}
