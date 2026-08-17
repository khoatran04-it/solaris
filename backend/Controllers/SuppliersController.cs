using backend.DTOs;
using backend.DTOs.SupplierDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Controller quản lý thực thể Nhà cung cấp (Suppliers).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách rút gọn toàn bộ nhà cung cấp.
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<SupplierReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SupplierReadDto>>> GetAllSuppliers()
        {
            var result = await _supplierService.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Truy vấn danh sách nhà cung cấp có phân trang và bộ lọc nâng cao.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SupplierReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<SupplierReadDto>>> GetSuppliers(
            [FromQuery] string? search,
            [FromQuery] string? supplierTypesId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _supplierService.GetPagedAsync(
                search, supplierTypesId, isActive, createdAt, updatedAt, pageIndex, pageSize);

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết thông tin một nhà cung cấp theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupplierReadDto>> GetSupplier(int id)
        {
            var result = await _supplierService.GetByIdAsync(id);

            if (result == null)
                return NotFound(new { Message = "Không tìm thấy thông tin nhà cung cấp." });

            return Ok(result);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới bản ghi nhà cung cấp.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(SupplierReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SupplierReadDto>> PostSuppliers([FromBody] SupplierCreateDto createDto)
        {
            try
            {
                var newId = await _supplierService.CreateAsync(createDto);
                var newSupplier = await _supplierService.GetByIdAsync(newId);

                return CreatedAtAction(nameof(GetSupplier), new { id = newId }, newSupplier);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp hiện có.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PutSupplier(int id, [FromBody] SupplierUpdateDto updateDto)
        {
            try
            {
                await _supplierService.UpdateAsync(id, updateDto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa bản ghi nhà cung cấp khỏi hệ thống.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                await _supplierService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Đổi trạng thái hoạt động của nhà cung cấp (Khóa/Mở khóa).
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _supplierService.ToggleActiveAsync(id);
                return Ok(new { Message = "Đã thay đổi trạng thái nhà cung cấp thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion
    }
}