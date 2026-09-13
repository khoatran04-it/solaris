using backend.DTOs;
using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Controllers.Shop
{
    /// <summary>
    /// API Yêu Cầu Đổi Trả Hàng Khách Hàng (Customer Storefront Returns & RMA).
    /// Cho phép khách hàng tự tạo yêu cầu đổi trả cho các đơn hàng đã nhận,
    /// tra cứu danh sách các yêu cầu RMA cá nhân và xem tiến độ đối soát hoàn tiền.
    /// </summary>
    [ApiController]
    [Route("api/shop/returns")]
    [Produces("application/json")]
    [Authorize]
    public class ShopReturnsController : ShopBaseController
    {
        private readonly IShopReturnService _returnService;

        public ShopReturnsController(IShopReturnService returnService)
        {
            _returnService = returnService;
        }

        /// <summary>
        /// Tạo Yêu cầu Đổi/Trả hàng mới (Self-Service RMA Request).
        /// </summary>
        /// <param name="request">Thông tin mã đơn hàng gốc, lý do trả và danh sách mặt hàng kèm số lượng muốn hoàn trả.</param>
        /// <returns>Dữ liệu phiếu trả hàng vừa được tạo ở trạng thái Chờ tiếp nhận (Pending).</returns>
        /// <response code="200">Gửi yêu cầu đổi trả hàng thành công.</response>
        /// <response code="400">Đơn hàng chưa được giao, danh sách mặt hàng rỗng hoặc số lượng không hợp lệ.</response>
        /// <response code="404">Không tìm thấy đơn hàng tương ứng của khách hàng.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ShopReturnReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ShopReturnReadDto>> CreateReturn([FromBody] ShopReturnCreateRequestDto request)
        {
            try
            {
                int customerId = GetCurrentCustomerId();
                var ret = await _returnService.CreateReturnRequestAsync(customerId, request);
                return Ok(ret);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = errorMsg });
            }
        }

        /// <summary>
        /// Lấy danh sách Phiếu yêu cầu đổi/trả hàng của khách hàng hiện tại (có phân trang).
        /// </summary>
        /// <param name="pageIndex">Số thứ tự trang (mặc định 1).</param>
        /// <param name="pageSize">Số bản ghi trên mỗi trang (mặc định 10).</param>
        /// <returns>Danh sách phiếu trả hàng của khách hàng kèm thông tin phân trang.</returns>
        /// <response code="200">Truy vấn danh sách yêu cầu đổi trả thành công.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ShopReturnReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PagedResult<ShopReturnReadDto>>> GetReturns([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            int customerId = GetCurrentCustomerId();
            var returns = await _returnService.GetCustomerReturnsAsync(customerId, pageIndex, pageSize);
            return Ok(returns);
        }

        /// <summary>
        /// Xem chi tiết một Phiếu yêu cầu đổi/trả hàng theo Mã phiếu (ReturnCode).
        /// </summary>
        /// <param name="returnCode">Mã phiếu đổi trả (ví dụ: RET-20260830-AB12).</param>
        /// <returns>Thông tin chi tiết phiếu trả hàng, kết quả kiểm định QC từng món và số tiền hoàn trả.</returns>
        /// <response code="200">Tìm thấy và trả về chi tiết phiếu đổi trả.</response>
        /// <response code="404">Không tìm thấy phiếu đổi trả hoặc phiếu không thuộc khách hàng hiện tại.</response>
        /// <response code="401">Khách hàng chưa đăng nhập.</response>
        [HttpGet("{returnCode}")]
        [ProducesResponseType(typeof(ShopReturnReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
