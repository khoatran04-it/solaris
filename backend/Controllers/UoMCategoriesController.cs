using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Danh mục Nhóm đơn vị tính (Unit of Measure Categories).
    /// Hỗ trợ quản lý các nhóm đơn vị (như Khối lượng, Thể tích) và các đơn vị cơ sở liên quan.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UoMCategoriesController : ControllerBase
    {
        private readonly IUoMCategoryService _service;

        public UoMCategoriesController(IUoMCategoryService service)
        {
            _service = service;
        }

        #region Truy vấn (Query Endpoints)

        /// <summary>
        /// Lấy danh sách nhóm đơn vị tính có phân trang và bộ lọc.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UoMCategoryReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm đơn vị tính (không phân trang).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<UoMCategoryReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var result = await _service.GetAllListAsync(isActive ?? false);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhóm đơn vị tính theo ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UoMCategoryReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy nhóm đơn vị tính yêu cầu." });

            return Ok(result);
        }

        #endregion

        #region Thao tác Dữ liệu (Command Endpoints)

        /// <summary>
        /// Thêm mới một nhóm đơn vị tính vào hệ thống.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UoMCategoryReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UoMCategoryCreateDto dto)
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
        /// Cập nhật thông tin nhóm đơn vị tính hiện có.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UoMCategoryUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật nhóm ĐVT thành công." });
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
        /// Xóa bỏ nhóm đơn vị tính khỏi hệ thống.
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
                return Ok(new { message = "Xóa nhóm ĐVT thành công." });
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
        /// Đảo ngược trạng thái hoạt động (Kích hoạt/Khóa) của nhóm đơn vị.
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
                return Ok(new { message = "Cập nhật trạng thái hoạt động thành công." });
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