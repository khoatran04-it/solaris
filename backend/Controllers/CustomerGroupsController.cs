using backend.DTOs;
using backend.DTOs.CustomerGroupDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Controller quản lý các Nhóm khách hàng (Customer Groups).
    /// Cung cấp các chức năng tìm kiếm nâng cao, quản lý thông tin và trạng thái hoạt động.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerGroupsController : ControllerBase
    {
        private readonly ICustomerGroupService _service;

        public CustomerGroupsController(ICustomerGroupService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy toàn bộ danh sách nhóm khách hàng (không phân trang).
        /// </summary>
        /// <remarks>Sử dụng cho các thành phần chọn nhanh hoặc Dropdown trên UI.</remarks>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<CustomerGroupReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var data = await _service.GetAllListAsync();
            return Ok(data);
        }

        /// <summary>
        /// Lấy danh sách nhóm khách hàng có phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Tìm kiếm theo Mã hoặc Tên nhóm.</param>
        /// <param name="names">Lọc danh sách tên cụ thể (ngăn cách bằng dấu phẩy).</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động (true/false).</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerGroupReadDto>), StatusCodes.Status200OK)]
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
        /// Lấy chi tiết thông tin một nhóm khách hàng theo ID định danh.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerGroupReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { Message = "Không tìm thấy nhóm khách hàng này trên hệ thống." });
            }
            return Ok(data);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một nhóm khách hàng.
        /// </summary>
        /// <response code="200">Trả về thông báo và ID của nhóm vừa tạo.</response>
        /// <response code="400">Lỗi nếu mã nhóm đã tồn tại hoặc dữ liệu không hợp lệ.</response>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CustomerGroupCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới nhóm khách hàng thành công", Id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin chi tiết của một nhóm khách hàng.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerGroupUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật thông tin thành công" });
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
        /// Xóa bỏ một nhóm khách hàng (Hỗ trợ cơ chế Soft Delete).
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
                return Ok(new { Message = "Xóa nhóm khách hàng thành công" });
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
        // SECTION: SPECIAL BUSINESS ACTIONS
        // ==========================================
        #region Business Actions

        /// <summary>
        /// Thay đổi nhanh trạng thái hoạt động (Kích hoạt/Khóa) của một nhóm khách hàng.
        /// </summary>
        [HttpPut("{id}/toggle-active")]
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
            // Logic bảo mật: Bắt mọi Exception để tránh rò rỉ thông tin stack trace (Lỗi 500)
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion
    }
}