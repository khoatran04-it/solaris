using backend.DTOs;
using backend.DTOs.UoMDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý danh mục Đơn vị tính (Unit of Measure - UoM).
    /// Cung cấp các thao tác quản lý đơn vị quy đổi, trạng thái hoạt động và tìm kiếm thông minh.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UoMsController : ControllerBase
    {
        private readonly IUoMService _service;

        public UoMsController(IUoMService service)
        {
            _service = service;
        }

        #region Truy vấn (Query Endpoints)

        /// <summary>
        /// Truy vấn danh sách đơn vị tính có phân trang và bộ lọc nâng cao.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UoMReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, categoryId, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách đơn vị tính không phân trang.
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<UoMReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var result = await _service.GetAllListAsync(isActive ?? false);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn vị tính theo ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UoMReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy đơn vị tính yêu cầu." });

            return Ok(result);
        }

        #endregion

        #region Thao tác Dữ liệu (Command Endpoints)

        /// <summary>
        /// Thêm mới một đơn vị tính vào hệ thống.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UoMReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UoMCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                var created = await _service.GetByIdAsync(id);
                return CreatedAtAction(nameof(GetById), new { id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết đơn vị tính.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UoMUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật đơn vị tính thành công." });
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
        /// Xóa bỏ đơn vị tính khỏi hệ thống.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa đơn vị tính thành công." });
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
        /// Thay đổi nhanh trạng thái hoạt động (Bật/Khóa) của đơn vị tính.
        /// </summary>
        [HttpPatch("{id:int}/toggle-active")]
        [HttpPut("{id:int}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _service.ToggleActiveAsync(id);
                return Ok(new { message = "Đã cập nhật trạng thái hoạt động." });
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