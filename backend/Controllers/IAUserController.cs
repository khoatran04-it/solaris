using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản trị Nhân viên & Người dùng hệ thống (IAM).
    /// </summary>
    [Route("api/ia-users")]
    [ApiController]
    [Authorize]
    public class IAUserController : ControllerBase
    {
        private readonly IIAUserService _userService;

        public IAUserController(IIAUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Lấy danh sách nhân viên có phân trang, tìm kiếm và lọc.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? roleId,
            [FromQuery] int? warehouseId,
            [FromQuery] bool? isActive,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _userService.GetPagedAsync(search, roleId, warehouseId, isActive, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách nhân viên phục vụ dropdown.
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _userService.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết nhân viên theo ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _userService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới tài khoản nhân viên.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] IAUserCreateDto dto)
        {
            try
            {
                var id = await _userService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, new { message = "Tạo tài khoản nhân viên thành công.", id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ nhân viên.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] IAUserUpdateDto dto)
        {
            try
            {
                await _userService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật nhân viên thành công." });
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
        /// Đổi hoặc cấp lại mật khẩu cho nhân viên.
        /// </summary>
        [HttpPatch("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] IAUserChangePasswordDto dto)
        {
            try
            {
                await _userService.ChangePasswordAsync(id, dto);
                return Ok(new { message = "Đổi mật khẩu thành công." });
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
        /// Xóa mềm tài khoản nhân viên.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return Ok(new { message = "Xóa nhân viên thành công." });
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
        /// Bật / tắt trạng thái kích hoạt của tài khoản.
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _userService.ToggleActiveAsync(id);
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