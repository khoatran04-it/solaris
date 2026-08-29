using backend.DTOs;
using backend.DTOs.CustomerGroupDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Controller quản lý các Nhóm khách hàng (Customer Groups).
    /// Cung cấp các chức năng tìm kiếm nâng cao, quản lý thông tin và trạng thái hoạt động.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class CustomerGroupsController : ControllerBase
    {
        private readonly ICustomerGroupService _service;

        public CustomerGroupsController(ICustomerGroupService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm khách hàng (không phân trang).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<CustomerGroupReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var data = await _service.GetAllListAsync(isActive ?? false);
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách nhóm khách hàng có phân trang và bộ lọc nâng cao.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerGroupReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? names,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, names, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin một nhóm khách hàng theo ID định danh.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerGroupReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { message = "Không tìm thấy nhóm khách hàng này trên hệ thống." });
            }
            return Ok(data);
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một nhóm khách hàng.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerGroupReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CustomerGroupCreateDto dto)
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết của một nhóm khách hàng.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerGroupUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
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
        /// Xóa bỏ một nhóm khách hàng (Hỗ trợ cơ chế Soft Delete).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent();
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

        /// <summary>
        /// Thay đổi nhanh trạng thái hoạt động (Kích hoạt/Khóa) của một nhóm khách hàng.
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _service.ToggleActiveAsync(id);
                return Ok(new { message = "Đã cập nhật trạng thái hoạt động thành công." });
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