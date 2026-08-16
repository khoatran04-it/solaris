using backend.DTOs.InventoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/warehouses")]
    [ApiController]
    // [Authorize] // Bật lên sau khi ghép bảo mật
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        // Dùng cho Dropdown khi tạo phiếu, hoặc gán User (Chỉ lấy kho đang Active)
        [HttpGet("all")]
        // [RequirePermission("INVENTORY_VIEW")]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _warehouseService.GetAllListAsync();
            return Ok(result);
        }

        // Dùng cho bảng danh sách có phân trang và lọc
        [HttpGet]
        // [RequirePermission("INVENTORY_VIEW")]
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

        [HttpGet("{id}")]
        // [RequirePermission("INVENTORY_VIEW")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _warehouseService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        // [RequirePermission("INVENTORY_MANAGE")]
        public async Task<IActionResult> Create([FromBody] WarehouseCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var id = await _warehouseService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Thêm mới kho hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        // [RequirePermission("INVENTORY_MANAGE")]
        public async Task<IActionResult> Update(int id, [FromBody] WarehouseUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _warehouseService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật kho hàng thành công." });
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

        [HttpDelete("{id}")]
        // [RequirePermission("INVENTORY_MANAGE")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _warehouseService.DeleteAsync(id);
                return Ok(new { message = "Xóa kho hàng thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/toggle-active")]
        // [RequirePermission("INVENTORY_MANAGE")]
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
        }
    }
}