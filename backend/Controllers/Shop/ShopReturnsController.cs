using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/returns")]
    [Authorize]
    public class ShopReturnsController : ShopBaseController
    {
        private readonly IShopReturnService _returnService;

        public ShopReturnsController(IShopReturnService returnService)
        {
            _returnService = returnService;
        }

        /// <summary>
        /// Tạo yêu cầu đổi/trả hàng mới (RMA)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ShopReturnReadDto>> CreateReturn([FromBody] ShopReturnCreateRequestDto request)
        {
            int customerId = GetCurrentCustomerId();
            var ret = await _returnService.CreateReturnRequestAsync(customerId, request);
            return Ok(ret);
        }

        /// <summary>
        /// Danh sách phiếu yêu cầu đổi/trả hàng của khách
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<ShopReturnReadDto>>> GetReturns([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            int customerId = GetCurrentCustomerId();
            var returns = await _returnService.GetCustomerReturnsAsync(customerId, pageIndex, pageSize);
            return Ok(returns);
        }

        /// <summary>
        /// Chi tiết 1 phiếu yêu cầu đổi/trả hàng
        /// </summary>
        [HttpGet("{returnCode}")]
        public async Task<ActionResult<ShopReturnReadDto>> GetReturnByCode(string returnCode)
        {
            int customerId = GetCurrentCustomerId();
            var ret = await _returnService.GetReturnByCodeAsync(customerId, returnCode);
            if (ret == null)
                return NotFound(new { message = "Không tìm thấy phiếu trả hàng." });

            return Ok(ret);
        }
    }
}
