using backend.DTOs;
using backend.DTOs.InventoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /// <summary>
    /// API Tra cứu & Quản lý Sổ cái Tồn kho 4 ngăn (Warehouse Inventory 4-Bucket Ledger).
    /// Hỗ trợ phân quyền dữ liệu theo danh sách kho được gán cho nhân viên (Data-Level Authorization).
    /// </summary>
    [Route("api/inventories")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoriesController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Helper trích xuất danh sách ID kho mà nhân viên có quyền truy cập từ JWT Token Claims.
        /// </summary>
        private List<int>? GetAllowedWarehouseIds()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "WarehouseIds");
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int id) ? id : 0)
                    .Where(id => id > 0)
                    .ToList();
            }

            return null; // Mặc định nếu không có claim giới hạn thì xem toàn cục (SuperAdmin / HQ)
        }

        /// <summary>
        /// Lấy toàn bộ danh sách số dư tồn kho (phục vụ xuất Excel báo cáo hoặc đồng bộ số liệu).
        /// </summary>
        /// <returns>Danh sách các dòng tồn kho theo định dạng rút gọn.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<InventoryReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllList()
        {
            var allowedWarehouses = GetAllowedWarehouseIds();
            var result = await _inventoryService.GetAllListAsync(allowedWarehouses);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách tồn kho theo thời gian thực có phân trang và bộ lọc chuyên sâu.
        /// </summary>
        /// <param name="search">Từ khóa tìm kiếm theo mã SKU, tên sản phẩm hoặc mã lô hàng.</param>
        /// <param name="warehouseId">ID kho hàng cần lọc.</param>
        /// <param name="isExpiringSoon">Lọc các lô hàng sắp hết hạn trong vòng 7 ngày.</param>
        /// <param name="isOutOfStock">Lọc các mặt hàng có số dư khả dụng bằng 0.</param>
        /// <param name="pageIndex">Trang hiện tại (bắt đầu từ 1, mặc định: 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định: 10).</param>
        /// <returns>Kết quả phân trang danh sách tồn kho.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<InventoryReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] int? warehouseId,
            [FromQuery] bool? isExpiringSoon,
            [FromQuery] bool? isOutOfStock,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var allowedWarehouses = GetAllowedWarehouseIds();

            var result = await _inventoryService.GetPagedAsync(
                search,
                warehouseId,
                isExpiringSoon,
                isOutOfStock,
                pageIndex,
                pageSize,
                allowedWarehouses
            );

            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết số dư tồn kho của một dòng theo ID.
        /// </summary>
        /// <param name="id">ID dòng tồn kho trong kho lưu trữ.</param>
        /// <returns>Dữ liệu chi tiết dòng tồn kho.</returns>
        /// <response code="200">Truy vấn thành công.</response>
        /// <response code="401">Chưa xác thực người dùng.</response>
        /// <response code="404">Không tìm thấy bản ghi tồn kho hoặc không có quyền truy cập.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(InventoryReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var allowedWarehouses = GetAllowedWarehouseIds();
                var result = await _inventoryService.GetByIdAsync(id, allowedWarehouses);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}