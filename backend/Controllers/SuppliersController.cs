using backend.DTOs;
using backend.DTOs.SupplierDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Controller quản lý thực thể Nhà cung cấp (Suppliers).
    /// Hỗ trợ tìm kiếm nâng cao, lọc đa chiều, quản lý hồ sơ và bảo toàn dữ liệu chuỗi cung ứng.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // ==========================================
        // SECTION: READ OPERATIONS (GET)
        // ==========================================
        #region Read Operations

        /// <summary>
        /// Lấy danh sách rút gọn toàn bộ nhà cung cấp (thường dùng cho Dropdown / Select).
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<SupplierReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SupplierReadDto>>> GetAllSuppliers()
        {
            var result = await _supplierService.GetAllListAsync();
            return Ok(result);
        }

        /// <summary>
        /// Truy vấn danh sách nhà cung cấp có phân trang và bộ lọc nâng cao.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã, tên hoặc số điện thoại.</param>
        /// <param name="supplierTypeIds">Danh sách ID phân loại nhà cung cấp phân cách bằng dấu phẩy (VD: "1,2,3").</param>
        /// <param name="isActive">Lọc theo trạng thái hoạt động.</param>
        /// <param name="createdAt">Lọc theo ngày tạo hồ sơ.</param>
        /// <param name="updatedAt">Lọc theo ngày cập nhật hồ sơ.</param>
        /// <param name="pageIndex">Chỉ số trang (bắt đầu từ 1).</param>
        /// <param name="pageSize">Số lượng bản ghi mỗi trang.</param>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SupplierReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<SupplierReadDto>>> GetSuppliers(
            [FromQuery] string? search,
            [FromQuery] string? supplierTypeIds,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _supplierService.GetPagedAsync(
                search, supplierTypeIds, isActive, createdAt, updatedAt, pageIndex, pageSize);

            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết hồ sơ một nhà cung cấp theo ID.
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SupplierReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupplierReadDto>> GetSupplierById(int id)
        {
            var result = await _supplierService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin nhà cung cấp." });
            }

            return Ok(result);
        }

        #endregion


        // ==========================================
        // SECTION: WRITE OPERATIONS (POST / PUT / DELETE / PATCH)
        // ==========================================
        #region Write Operations

        /// <summary>
        /// Tạo mới một hồ sơ nhà cung cấp.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo mới nhà cung cấp.</param>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierCreateDto dto)
        {
            try
            {
                var newId = await _supplierService.CreateAsync(dto);
                var createdSupplier = await _supplierService.GetByIdAsync(newId);
                return CreatedAtAction(nameof(GetSupplierById), new { id = newId }, createdSupplier);
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
        /// Cập nhật thông tin hồ sơ nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp cần cập nhật.</param>
        /// <param name="dto">Dữ liệu cập nhật mới.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SupplierUpdateDto dto)
        {
            try
            {
                await _supplierService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật thông tin nhà cung cấp thành công." });
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
        /// Xóa một nhà cung cấp (chống xóa nếu đã phát sinh dữ liệu liên kết).
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp cần xóa.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                await _supplierService.DeleteAsync(id);
                return Ok(new { message = "Xóa nhà cung cấp thành công." });
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
        /// Bật/Tắt trạng thái hoạt động (Kích hoạt / Tạm khóa) của nhà cung cấp.
        /// </summary>
        /// <param name="id">Mã định danh của nhà cung cấp.</param>
        [HttpPatch("{id}/toggle-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _supplierService.ToggleActiveAsync(id);
                return Ok(new { message = "Đã thay đổi trạng thái nhà cung cấp thành công." });
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