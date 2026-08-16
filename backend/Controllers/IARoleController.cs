using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/ia-roles")]
    [ApiController]
    // [Authorize] // Tạm comment lại để sếp dễ test bằng Swagger, test xong nhớ mở ra nhé!
    public class IARoleController : ControllerBase
    {
        // 🔥 Đã sửa: Tiêm Interface IIARoleService
        private readonly IIARoleService _roleService;

        public IARoleController(IIARoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] string? search, [FromQuery] bool? isActive, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _roleService.GetPagedAsync(search, isActive, pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _roleService.GetAllListAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _roleService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IARoleCreateDto dto)
        {
            try
            {
                var id = await _roleService.CreateAsync(dto);
                return Ok(new { message = "Tạo vai trò thành công.", id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] IARoleUpdateDto dto)
        {
            try
            {
                await _roleService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật vai trò thành công." });
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _roleService.DeleteAsync(id);
                return Ok(new { message = "Xóa vai trò thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _roleService.ToggleActiveAsync(id);
                return Ok(new { message = "Thay đổi trạng thái thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}