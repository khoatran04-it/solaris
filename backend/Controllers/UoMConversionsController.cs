using backend.DTOs;
using backend.DTOs.UoMConversionDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý các quy tắc Quy đổi Đơn vị tính.
    /// Hỗ trợ thiết lập tỷ lệ quy đổi chung cho hệ thống và tỷ lệ quy đổi đặc thù cho từng Sản phẩm.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UoMConversionsController : ControllerBase
    {
        private readonly IUoMConversionService _service;

        public UoMConversionsController(IUoMConversionService service)
        {
            _service = service;
        }

        #region Truy vấn (Query Endpoints)

        /// <summary>
        /// Lấy danh sách quy tắc quy đổi có phân trang và bộ lọc nâng cao.
        /// </summary>
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
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<UoMConversionReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList([FromQuery] bool? isActive = null)
        {
            var result = await _service.GetAllListAsync(isActive ?? false);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một quy tắc quy đổi đơn vị theo ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UoMConversionReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu quy đổi yêu cầu." });

            return Ok(result);
        }

        #endregion

        #region Thao tác Dữ liệu (Command Endpoints)

        /// <summary>
        /// Tạo mới quy tắc quy đổi đơn vị tính.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(UoMConversionReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] UoMConversionCreateDto dto)
        {
            try
            {
                var id = await _service.CreateAsync(dto);
                var created = await _service.GetByIdAsync(id);
                return CreatedAtAction(nameof(GetById), new { id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin quy tắc quy đổi.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] UoMConversionUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật quy tắc quy đổi thành công." });
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

        /// <summary>
        /// Xóa vĩnh viễn một quy tắc quy đổi đơn vị tính.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Xóa quy tắc quy đổi thành công." });
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

        /// <summary>
        /// Đảo trạng thái hoạt động (Bật/Tắt) của quy tắc quy đổi.
        /// </summary>
        [HttpPatch("{id:int}/toggle-active")]
        [HttpPut("{id:int}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _service.ToggleActiveAsync(id);
                return Ok(new { message = "Cập nhật trạng thái hoạt động thành công." });
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