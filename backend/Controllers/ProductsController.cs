using backend.DTOs;
using backend.DTOs.ProductDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Sản phẩm cha (Product Master).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách sản phẩm không phân trang.
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<ProductReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var data = await _service.GetAllListAsync(isActive ?? false);
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách sản phẩm có phân trang, tìm kiếm và bộ lọc đa luồng.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? categoryId,
            [FromQuery] string? baseUoMId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, categoryId, baseUoMId, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một sản phẩm theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductReadDto), StatusCodes.Status200OK)]
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
        /// Lấy cấu hình các thuộc tính động (EAV Template) cho sản phẩm theo danh mục.
        /// </summary>
        [HttpGet("{id}/attributes-config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAttributesConfigForProduct(int id)
        {
            try
            {
                var data = await _service.GetDynamicAttributesConfigAsync(id);
                return Ok(data);
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
                return BadRequest(new { message = "Lỗi khi lấy cấu hình thuộc tính: " + ex.Message });
            }
        }

        #endregion

        // ==========================================
        // SECTION: WRITE OPERATIONS (POST, PUT, DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một sản phẩm gốc.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ProductReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
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
            catch (DbUpdateException dbEx)
            {
                return BadRequest(new { message = GetInnermostMessage(dbEx) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = GetInnermostMessage(ex) });
            }
        }

        /// <summary>
        /// Cập nhật thông tin sản phẩm gốc.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật sản phẩm thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                return BadRequest(new { message = GetInnermostMessage(dbEx) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = GetInnermostMessage(ex) });
            }
        }

        /// <summary>
        /// Xóa mềm một sản phẩm gốc.
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
                return Ok(new { message = "Xóa sản phẩm thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                return BadRequest(new { message = GetInnermostMessage(dbEx) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = GetInnermostMessage(ex) });
            }
        }

        #endregion

        // ==========================================
        // SECTION: SPECIAL BUSINESS ACTIONS (PATCH / PUT)
        // ==========================================
        #region Business Logic Actions

        /// <summary>
        /// Thay đổi trạng thái hiển thị (Đang bán / Ngừng bán) của sản phẩm.
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
                return Ok(new { message = "Thay đổi trạng thái thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException dbEx)
            {
                return BadRequest(new { message = GetInnermostMessage(dbEx) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = GetInnermostMessage(ex) });
            }
        }

        private static string GetInnermostMessage(Exception ex)
        {
            var current = ex;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }
            return current.Message;
        }

        #endregion
    }
}