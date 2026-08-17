using backend.DTOs.InventoryDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API Tra cứu & Quản lý Sổ cái Tồn kho 4 ngăn (Warehouse Inventory Ledger).
    /// </summary>
    [Route("api/inventories")]
    [ApiController]
    [Authorize]
    public class InventoriesController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoriesController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        // Helper trích xuất mảng ID kho từ Token đăng nhập của nhân viên
        private List<int>? GetAllowedWarehouseIds()
        {
            // Nếu là Admin tổng thì trả về null để Bypass
            // if (User.IsInRole("SYSTEM_ADMIN")) return null;

            var claim = User.Claims.FirstOrDefault(c => c.Type == "WarehouseIds");
            if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
            {
                return claim.Value.Split(',').Select(int.Parse).ToList();
            }

            // Mặc định không có quyền -> mảng rỗng
            return new List<int>();
        }

        // Lấy tất cả dữ liệu (Dành cho việc xuất Excel hoặc Report)
        [HttpGet("all")]
        // [RequirePermission("INVENTORY_VIEW")]
        public async Task<IActionResult> GetAllList()
        {
            var allowedWarehouses = GetAllowedWarehouseIds();
            var result = await _inventoryService.GetAllListAsync(allowedWarehouses);
            return Ok(result);
        }

        // Dùng cho bảng danh sách có phân trang và lọc (Convention trải phẳng tham số)
        [HttpGet]
        // [RequirePermission("INVENTORY_VIEW")]
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

        [HttpGet("{id}")]
        // [RequirePermission("INVENTORY_VIEW")]
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