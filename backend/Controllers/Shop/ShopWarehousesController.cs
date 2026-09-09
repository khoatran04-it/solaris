using backend.Data;
using backend.DTOs.ShopDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers.Shop
{
    /// <summary>
    /// API cung cấp danh sách Điểm phục vụ / Kho Bán Lẻ dành cho khách hàng Shop B2C (Store-Locking & Smart Routing).
    /// </summary>
    [ApiController]
    [Route("api/shop/warehouses")]
    [Produces("application/json")]
    public class ShopWarehousesController : ShopBaseController
    {
        private readonly SolarisDbContext _context;

        public ShopWarehousesController(SolarisDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách các Kho Bán Lẻ đang hoạt động kèm địa chỉ và tọa độ GPS để định vị kho gần nhất.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ShopWarehouseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ShopWarehouseDto>>> GetRetailWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.Address)
                .Where(w => w.IsActive && !w.IsDeleted && w.WarehouseType == "Kho Bán Lẻ")
                .OrderBy(w => w.Id)
                .Select(w => new ShopWarehouseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Code = w.Code,
                    WarehouseType = w.WarehouseType,
                    Province = w.Address != null ? w.Address.Province : null,
                    District = w.Address != null ? w.Address.District : null,
                    Ward = w.Address != null ? w.Address.Ward : null,
                    StreetAddress = w.Address != null ? w.Address.StreetAddress : null,
                    FullAddress = w.Address != null ? w.Address.FullAddress : null,
                    Latitude = w.Address != null ? w.Address.Latitude : null,
                    Longitude = w.Address != null ? w.Address.Longitude : null,
                    IsActive = w.IsActive
                })
                .ToListAsync();

            return Ok(warehouses);
        }
    }
}
