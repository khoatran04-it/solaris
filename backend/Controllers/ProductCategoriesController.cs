using backend.DTOs;
using backend.DTOs.ProductCategoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Endpoint quản lý Phân loại sản phẩm
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProductCategoriesController : ControllerBase
    {
        private readonly IProductCategoryService _service;

        public ProductCategoriesController(IProductCategoryService service)
        {
            _service = service;
        }

        // ==========================================
        // SECTION: READ ENDPOINTS (GET)
        // ==========================================

        #region Read Poerations

        /// <summary>
        /// Lấy danh sách rút gọn toàn bộ loại sản phẩm
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<ProductCategoryReadDto>), //API trả về một danh sách DTO.
            StatusCodes.Status200OK)] //status code mặc định là 200 OK.
        public async Task<IActionResult> GetAllList() //IActionResult là kiểu chung cho tất cả kết quả trả về từ controller (Ok, NotFound, BadRequest…).
        {
            var data = await _service.GetAllListAsync(); //IEnumerable<ProductCategoryReadDto>
            return Ok(data);//tạo ra một HTTP 200 OK response kèm dữ liệu. => client nhận được JSON chứa danh sách DTO.
        }

        /// <summary>
        /// Lấy danh sách loại sản phẩm có phân trang, lọc và tìm kiếm
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductCategoryReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(//Nói ASP.NET Core lấy giá trị từ query string của URL (phần sau ?)
            [FromQuery] string? search,
            [FromQuery] string? names,
            [FromQuery] string? categoryGroupId,
            [FromQuery] bool? isActive,
            [FromQuery] DateTime? createdAt,
            [FromQuery] DateTime? updatedAt,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetPagedAsync(search, names, categoryGroupId, isActive, createdAt, updatedAt, pageIndex, pageSize); //Code trong Service bắt đầu chạy để trả về PagedResult<T>
            return Ok(result); //Sau khi service xử lý xong kết quả trả về -> Chuỗi JSON kèm mã 200
        }

        /// <summary>
        /// Lấy thông tin chi tiết 1 loại sản phẩm theo ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProductCategoryReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
            {
                return NotFound(new { Message = "Không tìm thấy danh mục sản phẩm" });
            }
            return Ok(data);
        }

        #endregion

        // ==========================================
        // SECTION: WRITE ENDPOINTS (POST, PUT, DELETE)
        // ==========================================
        #region
        /// <summary>
        /// Tạo mới một loại danh mục
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ProductCategoryCreateDto dto)
        {
            try
            {
                int newId = await _service.CreateAsync(dto);
                return Ok(new { Message = "Thêm mới thành công", Id = newId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin loại sanh mục
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCategoryUpdateDto dto)
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
        /// Xóa vĩnh viễn một loại doanh mục
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
                return Ok(new { Message = "Xóa loại danh mục thành công" });
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
