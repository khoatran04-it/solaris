using backend.DTOs;
using backend.DTOs.SupplierTypeDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Endpoints quản lý Danh mục Phân loại Nhà cung cấp.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierTypesController : ControllerBase
    {
        private readonly ISupplierTypeService _service;

        public SupplierTypesController(ISupplierTypeService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ ENDPOINTS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách rút gọn toàn bộ phân loại (Thường dùng cho Dropdown/Select).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<SupplierTypeReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var data = await _service.GetAllListAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách phân loại có phân trang, lọc và tìm kiếm.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SupplierTypeReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] string? names,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, names, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một phân loại theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierTypeReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { Message = "Không tìm thấy phân loại nhà cung cấp này." });
            }
            return Ok(data);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE ENDPOINTS (POST, PUT, DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một phân loại nhà cung cấp.
        /// </summary>
        /// <response code="200">Trả về ID của bản ghi vừa tạo</response>
        /// <response code="400">Lỗi validation hoặc trùng mã Code</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] SupplierTypeCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới thành công", Id = newId });
            }
            catch (Exception ex)
            {
                // Note: Thực tế nên log error ở đây (Logger.LogError)
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin phân loại nhà cung cấp.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] SupplierTypeUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật thành công" });
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
        /// Xóa vĩnh viễn một phân loại nhà cung cấp.
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
                return Ok(new { Message = "Xóa thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Chặn lỗi xóa nếu có ràng buộc khóa ngoại (Foreign Key)
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion
    }
}