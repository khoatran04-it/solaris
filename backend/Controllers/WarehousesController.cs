using backend.DTOs;
using backend.DTOs.InventoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Kho Hàng & Hồ sơ Địa chỉ vật lý (Warehouses & Warehouse Addresses).
    /// </summary>
    [ApiController]
    [Route("api/warehouses")]
    [Authorize]
    [Produces("application/json")]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách kho hàng (không phân trang).
        /// Thường dùng cho các Dropdown/Lookup khi tạo phiếu xuất/nhập/chuyển kho hoặc gán quyền nhân viên.
        /// </summary>
        /// <param name="isActive">Lọc chỉ lấy các kho đang hoạt động nếu true.</param>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<WarehouseReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var result = await _warehouseService.GetAllListAsync(isActive ?? false);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách kho hàng có hỗ trợ phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Tìm kiếm theo Mã hoặc Tên kho.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="province">Lọc theo Tỉnh/Thành phố.</param>
        /// <param name="pageIndex">Chỉ số trang hiện tại (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi trên mỗi trang.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<WarehouseReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] string? province,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _warehouseService.GetPagedAsync(search, isActive, province, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một kho hàng theo ID (bao gồm thông tin địa chỉ và trưởng kho).
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(WarehouseReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _warehouseService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu kho hàng." });

            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin trạng thái sức chứa tức thời (CBM, Tải trọng, % lấp đầy) của kho hàng.
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng.</param>
        [HttpGet("{id}/capacity")]
        [ProducesResponseType(typeof(WarehouseCapacityStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCapacity(int id)
        {
            try
            {
                var result = await _warehouseService.GetCapacityStatusAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một kho hàng và khởi tạo địa chỉ vật lý trong một Transaction.
        /// </summary>
        /// <param name="dto">Dữ liệu yêu cầu tạo mới kho hàng và địa chỉ.</param>
        [HttpPost]
        [ProducesResponseType(typeof(WarehouseReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] WarehouseCreateDto dto)
        {
            try
            {
                var id = await _warehouseService.CreateAsync(dto);
                var created = await _warehouseService.GetByIdAsync(id);
                return CreatedAtAction(nameof(GetById), new { id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin cấu hình kho hàng và đồng bộ địa chỉ vật lý.
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] WarehouseUpdateDto dto)
        {
            try
            {
                await _warehouseService.UpdateAsync(id, dto);
                return NoContent();
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Xóa (mềm) một kho hàng và địa chỉ vật lý (có kiểm tra 8 tầng khiên an toàn tồn kho và chứng từ).
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng cần xóa.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _warehouseService.DeleteAsync(id);
                return NoContent();
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Chuyển đổi trạng thái Hoạt động / Tạm đóng của kho hàng.
        /// </summary>
        /// <param name="id">Mã định danh của kho hàng.</param>
        [HttpPatch("{id}/toggle-active")]
        [HttpPut("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _warehouseService.ToggleActiveAsync(id);
                return Ok(new { message = "Thay đổi trạng thái hoạt động thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi hệ thống.", detail = ex.Message });
            }
        }

        #endregion
    }
}