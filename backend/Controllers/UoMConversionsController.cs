using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý các quy tắc Quy đổi Đơn vị tính.
    /// Hỗ trợ thiết lập tỷ lệ quy đổi chung cho hệ thống và tỷ lệ quy đổi đặc thù cho từng Sản phẩm.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UoMConversionsController : ControllerBase
    {
        private readonly IUoMConversionService _service;

        public UoMConversionsController(IUoMConversionService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách quy tắc quy đổi có phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Tìm kiếm theo tên Đơn vị tính hoặc Sản phẩm.</param>
        /// <param name="productId">Lọc quy đổi riêng của một sản phẩm.</param>
        /// <param name="isStandard">true: Lấy quy đổi hệ thống | false: Lấy quy đổi theo sản phẩm.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UoMConversionReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? productId,
            [FromQuery] bool? isStandard,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, productId, isStandard, isActive, createdAt, updatedAt, pageIndex, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách quy tắc quy đổi (không phân trang).
        /// </summary>
        /// <remarks>Thường dùng để kiểm tra dữ liệu hoặc đổ vào các bộ lọc tổng hợp ở Frontend.</remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<UoMConversionReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _service.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một quy tắc quy đổi đơn vị theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UoMConversionReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { Message = "Không tìm thấy dữ liệu quy đổi yêu cầu." });
            return Ok(result);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới quy tắc quy đổi đơn vị tính.
        /// </summary>
        /// <remarks>Hệ thống sẽ kiểm tra tính hợp lệ về nhóm đơn vị và chặn quy đổi vòng lặp.</remarks>
        /// <response code="200">Thành công, trả về ID của quy tắc vừa tạo.</response>
        /// <response code="400">Lỗi logic (tỷ lệ âm, khác nhóm đơn vị, hoặc trùng lặp).</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UoMConversionCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới quy tắc chuyển đổi thành công", Id = id });
            }
            catch (Exception ex)
            {
                // Note: Exception này bao gồm các lỗi validation logic chuyên sâu từ Service
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin quy tắc quy đổi.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UoMConversionUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật quy tắc chuyển đổi thành công" });
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
        /// Xóa vĩnh viễn một quy tắc quy đổi đơn vị tính.
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
                return Ok(new { Message = "Xóa quy tắc chuyển đổi thành công" });
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
        /// Đảo trạng thái hoạt động (Bật/Tắt) của quy tắc quy đổi.
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