using backend.DTOs;
using backend.DTOs.CustomerTierDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Controller quản lý các Bậc hạng khách hàng (Customer Loyalty Tiers).
    /// Thiết lập các chính sách về mức chi tiêu tối thiểu và quyền lợi tương ứng.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerTiersController : ControllerBase
    {
        private readonly ICustomerTierService _service;

        public CustomerTiersController(ICustomerTierService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (QUERIES)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách bậc hạng thành viên (không phân trang).
        /// </summary>
        /// <remarks>Dữ liệu được sắp xếp theo mức chi tiêu từ thấp đến cao.</remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<CustomerTierReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var data = await _service.GetAllListAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách bậc hạng có hỗ trợ tìm kiếm và phân trang.
        /// </summary>
        /// <param name="search">Tìm kiếm theo Mã hoặc Tên hạng.</param>
        /// <param name="names">Lọc theo danh sách tên cụ thể (phân tách bằng dấu phẩy).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerTierReadDto>), StatusCodes.Status200OK)]
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
        /// Lấy thông tin chi tiết của một hạng thành viên theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerTierReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { Message = "Không tìm thấy hạng thành viên yêu cầu." });
            }
            return Ok(data);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (COMMANDS)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một bậc hạng thành viên.
        /// </summary>
        /// <response code="200">Thành công, trả về ID hạng vừa tạo.</response>
        /// <response code="400">Lỗi nghiệp vụ (ví dụ: trùng mã Code).</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CustomerTierCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới hạng thành viên thành công", Id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin cấu hình của một bậc hạng.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerTierUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật dữ liệu thành công" });
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
        /// Xóa bậc hạng khỏi hệ thống (Hỗ trợ Soft Delete).
        /// </summary>
        /// <remarks>Dữ liệu khách hàng cũ thuộc hạng này sẽ được giữ lại nhờ cơ chế xóa mềm.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { Message = "Xóa hạng thành viên thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Thường lỗi do ràng buộc dữ liệu nếu không sử dụng Soft Delete triệt để
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion
    }
}