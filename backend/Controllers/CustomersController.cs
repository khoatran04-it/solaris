using backend.DTOs;
using backend.DTOs.CustomerDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Khách hàng (Customers).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _service;

        public CustomersController(ICustomerService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách khách hàng (không phân trang).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<CustomerReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var data = await _service.GetAllListAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách khách hàng có hỗ trợ phân trang và bộ lọc nâng cao.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? customerTypeId,
            [FromQuery] string? customerTierId,
            [FromQuery] string? customerGroupId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, customerTypeId, customerTierId, customerGroupId, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin hồ sơ một khách hàng theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound(new { message = "Không tìm thấy khách hàng." });

            return Ok(data);
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một hồ sơ khách hàng.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
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
        /// Cập nhật thông tin hồ sơ khách hàng.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
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
        /// Xóa (mềm) hồ sơ khách hàng khỏi hệ thống.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        /// Chuyển đổi trạng thái hoạt động (Cho phép / Khóa) tài khoản khách hàng.
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _service.ToggleActiveAsync(id);
                return Ok(new { message = "Thay đổi trạng thái hoạt động thành công." });
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