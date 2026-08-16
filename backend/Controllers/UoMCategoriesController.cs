using backend.DTOs;
using backend.DTOs.UoMCategoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý Danh mục Nhóm đơn vị tính (Unit of Measure Categories).
    /// Hỗ trợ quản lý các nhóm đơn vị (như Khối lượng, Thể tích) và các đơn vị cơ sở liên quan.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UoMCategoriesController : ControllerBase
    {
        private readonly IUoMCategoryService _service;

        public UoMCategoriesController(IUoMCategoryService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách nhóm đơn vị tính có phân trang và bộ lọc.
        /// </summary>
        /// <param name="search">Tìm kiếm theo Mã hoặc Tên nhóm.</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
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
        /// <remarks>Thường dùng để đổ dữ liệu vào các Dropdown/Select ở Frontend.</remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<UoMCategoryReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _service.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhóm đơn vị tính theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UoMCategoryReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { Message = "Không tìm thấy nhóm đơn vị tính yêu cầu." });

            return Ok(result);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Thêm mới một nhóm đơn vị tính vào hệ thống.
        /// </summary>
        /// <response code="200">Trả về ID của bản ghi vừa tạo.</response>
        /// <response code="400">Lỗi nếu Mã hoặc Tên đã tồn tại.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UoMCategoryCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới nhóm đơn vị tính thành công", Id = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhóm đơn vị tính hiện có.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UoMCategoryUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật nhóm đơn vị tính thành công" });
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
        /// Xóa bỏ nhóm đơn vị tính khỏi hệ thống.
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
                return Ok(new { Message = "Xóa nhóm đơn vị tính thành công" });
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


        // ==========================================
        // SECTION: SPECIAL BUSINESS ACTIONS (PATCH)
        // ==========================================
        #region Business Logic Actions

        /// <summary>
        /// Đảo ngược trạng thái hoạt động (Kích hoạt/Khóa) của nhóm đơn vị.
        /// </summary>
        [HttpPatch("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _service.ToggleActiveAsync(id);
                return Ok(new { Message = "Cập nhật trạng thái hoạt động thành công" });
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