using backend.DTOs;
using backend.DTOs.ProductVariantDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Biến thể sản phẩm (SKU - Stock Keeping Unit).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class ProductVariantsController : ControllerBase
    {
        private readonly IProductVariantService _service;

        public ProductVariantsController(IProductVariantService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách biến thể sản phẩm không phân trang.
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<ProductVariantReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var data = await _service.GetAllListAsync(isActive ?? false);
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách biến thể có phân trang, tìm kiếm và bộ lọc đa tiêu chí.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductVariantReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? productId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, productId, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một biến thể (kèm bảng giá và thuộc tính EAV) theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductVariantReadDto), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Tạo mới một biến thể sản phẩm (SKU).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProductVariantReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ProductVariantCreateDto dto)
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
        /// Cập nhật thông tin biến thể sản phẩm (kèm bảng giá và thuộc tính).
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductVariantUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật biến thể sản phẩm thành công" });
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
        /// Xóa một biến thể sản phẩm.
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
                return Ok(new { message = "Xóa biến thể sản phẩm thành công" });
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
        /// Thay đổi trạng thái hoạt động (Đang bán / Tạm khóa) của biến thể.
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
                return Ok(new { message = "Thay đổi trạng thái biến thể thành công" });
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
    }
}