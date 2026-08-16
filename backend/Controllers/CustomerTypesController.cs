using backend.DTOs;
using backend.DTOs.CustomerTypeDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Controller quản lý các phân loại khách hàng (Nhóm khách hàng).
    /// Cung cấp các thao tác truy vấn, phân trang và quản lý dữ liệu CustomerType.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerTypesController : ControllerBase
    {
        private readonly ICustomerTypeService _service;

        public CustomerTypesController(ICustomerTypeService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách phân loại khách hàng (không phân trang).
        /// </summary>
        /// <remarks>Thường được sử dụng cho các thành phần Dropdown hoặc Select ở Frontend.</remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<CustomerTypeReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var data = await _service.GetAllListAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách phân loại khách hàng có hỗ trợ tìm kiếm và phân trang.
        /// </summary>
        /// <param name="search">Tìm kiếm theo Mã hoặc Tên.</param>
        /// <param name="names">Lọc danh sách tên cụ thể (phân tách bằng dấu phẩy).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerTypeReadDto>), StatusCodes.Status200OK)]
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
        /// Lấy thông tin chi tiết một phân loại khách hàng theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerTypeReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { Message = "Không tìm thấy phân loại khách hàng yêu cầu." });
            }
            return Ok(data);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Thêm mới một phân loại khách hàng vào hệ thống.
        /// </summary>
        /// <response code="200">Trả về thông báo thành công và ID của bản ghi mới.</response>
        /// <response code="400">Trả về lỗi nếu mã phân loại bị trùng lặp hoặc dữ liệu không hợp lệ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CustomerTypeCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới phân loại thành công", Id = newId });
            }
            catch (Exception ex)
            {
                // Note: Exception có thể do trùng mã Code đã được xử lý ở tầng Service
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin phân loại khách hàng hiện có.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerTypeUpdateDto dto)
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
        /// Xóa phân loại khách hàng khỏi hệ thống (Hỗ trợ Soft Delete).
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
                return Ok(new { Message = "Xóa phân loại khách hàng thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Thường lỗi xảy ra khi bản ghi đang có ràng buộc khóa ngoại với các bảng Khách hàng
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion
    }
}