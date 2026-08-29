using backend.DTOs.CustomerAddressDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý sổ địa chỉ của khách hàng.
    /// Hỗ trợ các thao tác CRUD và thiết lập địa chỉ giao hàng mặc định.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class CustomerAddressesController : ControllerBase
    {
        private readonly ICustomerAddressService _service;

        public CustomerAddressesController(ICustomerAddressService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách toàn bộ địa chỉ của một khách hàng cụ thể.
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(IEnumerable<CustomerAddressReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var data = await _service.GetByCustomerIdAsync(customerId);
            return Ok(data);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một bản ghi địa chỉ theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerAddressReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound(new { message = "Không tìm thấy địa chỉ yêu cầu." });
            return Ok(data);
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới địa chỉ cho một khách hàng.
        /// </summary>
        [HttpPost("customer/{customerId}")]
        [ProducesResponseType(typeof(CustomerAddressReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(int customerId, [FromBody] CustomerAddressCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(customerId, dto);
                var created = await _service.GetByIdAsync(newId);
                return CreatedAtAction(nameof(GetById), new { id = newId }, created);
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
        /// Cập nhật nội dung địa chỉ hiện có.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerAddressUpdateDto dto)
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
        /// Xóa bỏ một địa chỉ (Hỗ trợ Soft Delete).
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

        #endregion

        // ==========================================
        // SECTION: BUSINESS LOGIC ACTIONS (PATCH)
        // ==========================================
        #region Business Logic Actions

        /// <summary>
        /// Cập nhật nhanh một địa chỉ cụ thể làm địa chỉ mặc định của khách hàng.
        /// </summary>
        [HttpPatch("{id}/set-default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetDefault(int id, [FromBody] SetDefaultRequest request)
        {
            try
            {
                await _service.SetDefaultAsync(id, request.CustomerId);
                return Ok(new { message = "Đã cập nhật địa chỉ mặc định thành công." });
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

        #endregion
    }

    public class SetDefaultRequest
    {
        public int CustomerId { get; set; }
    }
}