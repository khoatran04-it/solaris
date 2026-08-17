using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản trị Vai trò (Roles) và Phân quyền trong hệ thống.
    /// </summary>
    [Route("api/ia-roles")]
    [ApiController]
    [Authorize]
    public class IARoleController : ControllerBase
    {
        private readonly IIARoleService _roleService;

        public IARoleController(IIARoleService roleService)
        {
            _roleService = roleService;
        }

        /// <summary>
        /// Lấy danh sách vai trò có phân trang và tìm kiếm.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _roleService.GetPagedAsync(search, isActive, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách vai trò phục vụ dropdown.
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _roleService.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết vai trò theo ID.
        /// </summary>
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

        /// <summary>
        /// Tạo mới vai trò.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IARoleCreateDto dto)
        {
            try
            {
                var id = await _roleService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, new { message = "Tạo vai trò thành công.", id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin vai trò và danh sách quyền hạn.
        /// </summary>
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

        /// <summary>
        /// Xóa mềm vai trò.
        /// </summary>
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
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Bật / tắt trạng thái hoạt động của vai trò.
        /// </summary>
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
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}