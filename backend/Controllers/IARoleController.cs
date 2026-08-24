using backend.DTOs;
using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản trị Vai trò (Roles) và Cấu hình ma trận phân quyền trong hệ thống.
    /// </summary>
    [ApiController]
    [Route("api/ia-roles")]
    [Authorize]
    [Produces("application/json")]
    public class IARoleController : ControllerBase
    {
        private readonly IIARoleService _roleService;

        public IARoleController(IIARoleService roleService)
        {
            _roleService = roleService;
        }

        #region Truy vấn (Query Endpoints)
        /// <summary>
        /// Lấy danh sách vai trò có hỗ trợ tìm kiếm, lọc trạng thái và phân trang.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo tên hoặc mã vai trò.</param>
        /// <param name="isActive">Lọc theo trạng thái kích hoạt.</param>
        /// <param name="pageIndex">Trang hiện tại (mặc định: 1).</param>
        /// <param name="pageSize">Số bản ghi mỗi trang (mặc định: 10).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<IARoleReadDto>), StatusCodes.Status200OK)]
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
        /// Lấy toàn bộ danh sách vai trò đang hoạt động (phục vụ chọn Dropdown/Select).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<IARoleReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _roleService.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một vai trò theo ID (kèm danh sách mã quyền trực thuộc).
        /// </summary>
        /// <param name="id">Mã định danh vai trò.</param>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(IARoleReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        #endregion

        #region Thao tác Dữ liệu (Command Endpoints)
        /// <summary>
        /// Tạo mới vai trò và gán danh sách quyền hạn ban đầu.
        /// </summary>
        /// <param name="dto">Thông tin vai trò và danh sách quyền gán.</param>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Cập nhật thông tin vai trò và cấu hình lại danh sách quyền hạn.
        /// </summary>
        /// <param name="id">Mã định danh vai trò.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Xóa mềm vai trò khỏi hệ thống.
        /// </summary>
        /// <param name="id">Mã định danh vai trò cần xóa.</param>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Bật hoặc tắt trạng thái kích hoạt của vai trò.
        /// </summary>
        /// <param name="id">Mã định danh vai trò.</param>
        [HttpPatch("{id:int}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _roleService.ToggleActiveAsync(id);
                return Ok(new { message = "Thay đổi trạng thái vai trò thành công." });
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