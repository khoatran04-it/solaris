using backend.DTOs.CustomerAddressDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Quản lý sổ địa chỉ của khách hàng.
    /// Hỗ trợ các thao tác CRUD và thiết lập địa chỉ giao hàng mặc định.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerAddressesController : ControllerBase
    {
        private readonly ICustomerAddressService _service;

        public CustomerAddressesController(ICustomerAddressService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách toàn bộ địa chỉ của một khách hàng cụ thể.
        /// </summary>
        /// <param name="customerId">ID của khách hàng chủ quản.</param>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(IEnumerable<CustomerAddressReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var data = await _service.GetByCustomerIdAsync(customerId);
            return Ok(data);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một bản ghi địa chỉ theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerAddressReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound(new { Message = "Không tìm thấy địa chỉ yêu cầu." });
            return Ok(data);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới địa chỉ cho một khách hàng.
        /// </summary>
        /// <param name="customerId">ID khách hàng sở hữu địa chỉ.</param>
        /// <param name="dto">Dữ liệu địa chỉ chi tiết.</param>
        [HttpPost("customer/{customerId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create(int customerId, [FromBody] CustomerAddressCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(customerId, dto);
                return Ok(new { Message = "Thêm mới địa chỉ thành công", Id = newId });
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
        /// Cập nhật nội dung địa chỉ hiện có.
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerAddressUpdateDto dto)
        {
            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { Message = "Cập nhật thông tin địa chỉ thành công" });
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
        /// Xóa bỏ một địa chỉ (Hỗ trợ Soft Delete).
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
                return Ok(new { Message = "Xóa địa chỉ thành công" });
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
        // SECTION: BUSINESS LOGIC ACTIONS (PATCH)
        // ==========================================
        #region Business Logic Actions

        /// <summary>
        /// Cập nhật nhanh một địa chỉ cụ thể làm địa chỉ mặc định của khách hàng.
        /// </summary>
        /// <param name="id">ID của địa chỉ muốn đặt làm mặc định.</param>
        /// <param name="request">Body chứa CustomerId để xác thực quyền sở hữu.</param>
        [HttpPatch("{id}/set-default")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetDefault(int id, [FromBody] SetDefaultRequest request)
        {
            try
            {
                // Logic: Đảm bảo địa chỉ thuộc về đúng khách hàng được yêu cầu
                await _service.SetDefaultAsync(id, request.CustomerId);
                return Ok(new { Message = "Đã cập nhật địa chỉ mặc định" });
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

    // ==========================================
    // SECTION: HELPER MODELS
    // ==========================================
    #region Helper Models

    /// <summary>
    /// DTO rút gọn dùng cho request PATCH đặt địa chỉ mặc định.
    /// </summary>
    public class SetDefaultRequest
    {
        public int CustomerId { get; set; }
    }

    #endregion
}