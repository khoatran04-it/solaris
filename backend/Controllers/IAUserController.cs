using backend.DTOs;
using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản trị Tài khoản & Hồ sơ nhân viên trong hệ thống (IAM).
    /// </summary>
    [ApiController]
    [Route("api/ia-users")]
    [Authorize]
    [Produces("application/json")]
    public class IAUserController : ControllerBase
    {
        private readonly IIAUserService _userService;

        public IAUserController(IIAUserService userService)
        {
            _userService = userService;
        }

        #region Truy vấn (Query Endpoints)
        /// <summary>
        /// Lấy danh sách nhân viên có hỗ trợ tìm kiếm, lọc đa tiêu chí và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên, username, email, SĐT hoặc CCCD.</param>
        /// <param name="roleId">Lọc theo ID vai trò.</param>
        /// <param name="warehouseId">Lọc theo ID kho được phân quyền.</param>
        /// <param name="isActive">Lọc theo trạng thái kích hoạt.</param>
        /// <param name="pageIndex">Trang hiện tại (mặc định: 1).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định: 10).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<IAUserReadDto>), StatusCodes.Status200OK)]
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
        /// Lấy toàn bộ danh sách nhân viên đang hoạt động (phục vụ chọn Dropdown/Select).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<IAUserReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _userService.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một nhân viên theo ID (kèm Vai trò, Kho, Quyền ngoại lệ).
        /// </summary>
        /// <param name="id">Mã định danh nhân viên.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(IAUserReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        #endregion

        #region Thao tác Dữ liệu (Command Endpoints)
        /// <summary>
        /// Tạo mới tài khoản nhân viên và thiết lập phân quyền ban đầu.
        /// </summary>
        /// <param name="dto">Thông tin tài khoản và danh sách phân quyền.</param>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Cập nhật thông tin hồ sơ và cấu trúc phân quyền của nhân viên.
        /// </summary>
        /// <param name="id">Mã định danh nhân viên.</param>
        /// <param name="dto">Dữ liệu hồ sơ cập nhật mới.</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] IAUserUpdateDto dto)
        {
            try
            {
                await _userService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật thông tin nhân viên thành công." });
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
        /// Đổi hoặc cấp lại mật khẩu đăng nhập cho nhân viên.
        /// </summary>
        /// <param name="id">Mã định danh nhân viên.</param>
        /// <param name="dto">Mật khẩu mới.</param>
        [HttpPatch("{id:int}/change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] IAUserChangePasswordDto dto)
        {
            try
            {
                await _userService.ChangePasswordAsync(id, dto);
                return Ok(new { message = "Cập nhật mật khẩu thành công." });
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
        /// Xóa mềm tài khoản nhân viên khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh nhân viên cần xóa.</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return Ok(new { message = "Xóa tài khoản nhân viên thành công." });
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
        /// Bật hoặc tắt trạng thái kích hoạt của tài khoản nhân viên (Khóa / Mở khóa).
        /// </summary>
        /// <param name="id">Mã định danh nhân viên.</param>
        [HttpPatch("{id:int}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _userService.ToggleActiveAsync(id);
                return Ok(new { message = "Thay đổi trạng thái tài khoản thành công." });
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
        #endregion
    }
}