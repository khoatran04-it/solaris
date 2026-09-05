using backend.DTOs;
using backend.DTOs.CategoryAttributeDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Endpoints quản lý Cấu hình Thuộc tính cho Danh mục sản phẩm (Category Attribute Templates).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class CategoryAttributesController : ControllerBase
    {
        private readonly ICategoryAttributeService _service;

        public CategoryAttributesController(ICategoryAttributeService service)
        {
            _service = service;
        }

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy toàn bộ danh sách cấu hình thuộc tính của các danh mục.
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<CategoryAttributeReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var data = await _service.GetAllListAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách cấu hình thuộc tính có hỗ trợ phân trang, tìm kiếm và bộ lọc dữ liệu.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CategoryAttributeReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? categoryId,
            [FromQuery] string? attributeDefinitionId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, categoryId, attributeDefinitionId, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một bản ghi cấu hình thuộc tính theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CategoryAttributeReadDto), StatusCodes.Status200OK)]
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
        /// Lấy toàn bộ danh sách cấu hình thuộc tính của một Danh mục sản phẩm.
        /// </summary>
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(typeof(IEnumerable<CategoryAttributeReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategoryId(int categoryId)
        {
            var data = await _service.GetByCategoryIdAsync(categoryId);
            return Ok(data);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Đồng bộ hàng loạt ma trận thuộc tính cho Danh mục sản phẩm (Bulk Sync).
        /// </summary>
        [HttpPost("sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Sync([FromBody] CategoryAttributeSyncDto dto)
        {
            try
            {
                await _service.SyncCategoryAttributesAsync(dto);
                return Ok(new { message = "Đồng bộ thuộc tính cho danh mục thành công." });
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
        /// Thiết lập gán một thuộc tính từ Từ điển hệ thống vào một Danh mục sản phẩm.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CategoryAttributeCreateDto dto)
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
        /// Cập nhật cấu hình thuộc tính của danh mục.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryAttributeUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật cấu hình thuộc tính danh mục thành công." });
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
        /// Xóa bỏ cấu hình: Hủy gán thuộc tính khỏi danh mục sản phẩm.
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
                return Ok(new { message = "Xóa cấu hình thuộc tính danh mục thành công." });
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