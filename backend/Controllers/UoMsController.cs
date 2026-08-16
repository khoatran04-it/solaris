using backend.DTOs;
using backend.DTOs.UoMDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý danh mục Đơn vị tính (Unit of Measure - UoM).
    /// Cung cấp các thao tác quản lý đơn vị quy đổi, trạng thái hoạt động và tìm kiếm thông minh.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UoMsController : ControllerBase
    {
        private readonly IUoMService _service;

        public UoMsController(IUoMService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Truy vấn danh sách đơn vị tính có phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Tìm kiếm theo Mã, Tên hoặc Từ đồng nghĩa.</param>
        /// <param name="categoryId">Lọc theo nhóm đơn vị (Khối lượng, Chiều dài...).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UoMReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, categoryId, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách đơn vị tính không phân trang.
        /// </summary>
        /// <remarks>Thường dùng để đổ dữ liệu vào các thành phần chọn nhanh (Dropdown Select) trên giao diện.</remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<UoMReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _service.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn vị tính theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UoMReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { Message = "Không tìm thấy đơn vị tính yêu cầu." });

            return Ok(result);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Thêm mới một đơn vị tính vào hệ thống.
        /// </summary>
        /// <response code="200">Thành công, trả về ID bản ghi mới.</response>
        /// <response code="400">Lỗi nếu trùng mã hoặc nhóm đơn vị không tồn tại.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UoMCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới đơn vị tính thành công", Id = id });
            }
            catch (Exception ex)
            {
                // Note: Lỗi thường gặp là trùng Code hoặc vi phạm logic CategoryId
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết đơn vị tính.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UoMUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật đơn vị tính thành công" });
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
        /// Xóa bỏ đơn vị tính khỏi hệ thống.
        /// </summary>
        /// <remarks>Hệ thống sẽ chặn xóa nếu đơn vị này đang được dùng làm Đơn vị gốc (Base UoM) của một nhóm.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { Message = "Xóa đơn vị tính thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Chặn lỗi logic nghiệp vụ từ tầng Service
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion


        // ==========================================
        // SECTION: SPECIAL BUSINESS ACTIONS (PATCH)
        // ==========================================
        #region Business Logic Actions

        /// <summary>
        /// Thay đổi nhanh trạng thái hoạt động (Bật/Khóa) của đơn vị tính.
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
                return Ok(new { Message = "Đã cập nhật trạng thái hoạt động" });
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