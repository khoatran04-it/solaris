using backend.DTOs;
using backend.DTOs.ProductCategoryGroupDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Endpoints quản lý Danh mục Phân loại Nhóm ngành hàng (Product Category Groups).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class ProductCategoryGroupsController : ControllerBase
    {
        private readonly IProductCategoryGroupService _service;

        public ProductCategoryGroupsController(IProductCategoryGroupService service)
        {
            _service = service;
        }

        #region Truy vấn (Query)

        /// <summary>
        /// Lấy danh sách rút gọn toàn bộ nhóm danh mục (Thường dùng cho Dropdown/Select).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<ProductCategoryGroupReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var data = await _service.GetAllListAsync(isActive ?? false);
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách nhóm danh mục có phân trang, lọc và tìm kiếm.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductCategoryGroupReadDto>), StatusCodes.Status200OK)]
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
        /// Lấy thông tin chi tiết một nhóm danh mục theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductCategoryGroupReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { message = "Không tìm thấy nhóm ngành hàng này." });
            }
            return Ok(data);
        }

        #endregion

        #region Thao tác Dữ liệu (Command)

        /// <summary>
        /// Tạo mới một nhóm ngành hàng.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ProductCategoryGroupCreateDto dto)
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
        /// Cập nhật thông tin nhóm ngành hàng.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCategoryGroupUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật nhóm ngành hàng thành công." });
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
        /// Xóa bỏ một nhóm ngành hàng (Hỗ trợ Soft Delete).
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
                return Ok(new { message = "Xóa nhóm ngành hàng thành công." });
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
        /// Thay đổi trạng thái Hoạt động / Khóa của nhóm ngành hàng.
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
                return Ok(new { message = "Đã thay đổi trạng thái hoạt động của nhóm ngành hàng." });
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