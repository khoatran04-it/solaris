using backend.DTOs;
using backend.DTOs.AttributeDefinitionDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Endpoints quản lý Từ điển Thuộc tính động (EAV Attribute Definitions).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class AttributeDefinitionsController : ControllerBase
    {
        private readonly IAttributeDefinitionService _service;

        public AttributeDefinitionsController(IAttributeDefinitionService service)
        {
            _service = service;
        }

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy danh sách toàn bộ từ điển thuộc tính (phục vụ Dropdown / Select).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<AttributeDefinitionReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var data = await _service.GetAllListAsync(isActive ?? false);
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách từ điển thuộc tính có phân trang, lọc và tìm kiếm.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<AttributeDefinitionReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? dataType,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, dataType, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một định nghĩa thuộc tính theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(AttributeDefinitionReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);
                return Ok(data);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một định nghĩa thuộc tính vào từ điển hệ thống.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] AttributeDefinitionCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(dto);
                var created = await _service.GetByIdAsync(newId);
                return CreatedAtAction(nameof(GetById), new { id = newId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin định nghĩa thuộc tính.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] AttributeDefinitionUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật từ điển thuộc tính thành công." });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa bỏ một định nghĩa thuộc tính khỏi từ điển (Hỗ trợ Soft Delete).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa từ điển thuộc tính thành công." });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Thay đổi trạng thái Hoạt động / Khóa của định nghĩa thuộc tính.
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [HttpPut("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _service.ToggleActiveAsync(id);
                return Ok(new { message = "Đã thay đổi trạng thái từ điển thuộc tính." });
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