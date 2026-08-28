using backend.DTOs.SupplierAddressDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý địa chỉ kho của nhà cung cấp.
    /// Bao gồm các thao tác CRUD và quản lý trạng thái địa chỉ mặc định.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class SupplierAddressesController : ControllerBase
    {
        private readonly ISupplierAddressService _service;

        public SupplierAddressesController(ISupplierAddressService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách địa chỉ của một nhà cung cấp.
        /// </summary>
        /// <param name="supplierId">ID của nhà cung cấp cần lấy địa chỉ.</param>
        /// <returns>Danh sách địa chỉ (địa chỉ mặc định luôn đứng đầu).</returns>
        [HttpGet("supplier/{supplierId}")]
        [ProducesResponseType(typeof(IEnumerable<SupplierAddressReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SupplierAddressReadDto>>> GetBySupplierId(int supplierId)
        {
            var result = await _service.GetBySupplierIdAsync(supplierId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một địa chỉ theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierAddressReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupplierAddressReadDto>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy địa chỉ kho." });

            return Ok(result);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (CREATE / UPDATE / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Thêm mới địa chỉ kho cho nhà cung cấp.
        /// </summary>
        /// <param name="supplierId">ID nhà cung cấp chủ quản.</param>
        /// <param name="dto">Thông tin địa chỉ mới.</param>
        [HttpPost("{supplierId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(int supplierId, [FromBody] SupplierAddressCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(supplierId, dto);
                var createdAddress = await _service.GetByIdAsync(id);
                return CreatedAtAction(nameof(GetById), new { id }, createdAddress);
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
        /// Cập nhật thông tin chi tiết của một địa chỉ.
        /// </summary>
        /// <param name="id">ID của địa chỉ cần sửa.</param>
        /// <param name="dto">Dữ liệu cập nhật.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] SupplierAddressUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật địa chỉ kho thành công." });
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
        /// Xóa địa chỉ kho. Nếu xóa địa chỉ mặc định, hệ thống tự động chỉ định lại mặc định cho địa chỉ khác.
        /// </summary>
        /// <param name="id">ID địa chỉ cần xóa.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa địa chỉ kho thành công." });
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

        #endregion


        // ==========================================
        // SECTION: SPECIAL BUSINESS ACTIONS
        // ==========================================
        #region Business Logic Actions

        /// <summary>
        /// Chỉ định một địa chỉ cụ thể làm địa chỉ mặc định để giao dịch.
        /// </summary>
        /// <param name="id">ID địa chỉ muốn đặt làm mặc định.</param>
        /// <param name="supplierId">ID nhà cung cấp sở hữu địa chỉ đó.</param>
        [HttpPatch("{id}/set-default/{supplierId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetDefault(int id, int supplierId)
        {
            try
            {
                await _service.SetDefaultAsync(id, supplierId);
                return Ok(new { message = "Đã thay đổi địa chỉ mặc định thành công." });
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

        #endregion
    }
}