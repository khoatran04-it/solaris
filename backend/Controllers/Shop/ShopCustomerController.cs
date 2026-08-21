using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/customer")]
    [Authorize]
    public class ShopCustomerController : ShopBaseController
    {
        private readonly IShopCustomerService _customerService;

        public ShopCustomerController(IShopCustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Lấy hồ sơ thông tin cá nhân khách hàng
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<ShopCustomerProfileDto>> GetProfile()
        {
            int customerId = GetCurrentCustomerId();
            var profile = await _customerService.GetProfileAsync(customerId);
            return Ok(profile);
        }

        /// <summary>
        /// Cập nhật hồ sơ thông tin cá nhân
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<ShopCustomerProfileDto>> UpdateProfile([FromBody] ShopCustomerProfileUpdateDto request)
        {
            int customerId = GetCurrentCustomerId();
            var updated = await _customerService.UpdateProfileAsync(customerId, request);
            return Ok(updated);
        }

        /// <summary>
        /// Lấy danh sách sổ địa chỉ giao hàng
        /// </summary>
        [HttpGet("addresses")]
        public async Task<ActionResult<List<ShopAddressDto>>> GetAddresses()
        {
            int customerId = GetCurrentCustomerId();
            var addresses = await _customerService.GetAddressesAsync(customerId);
            return Ok(addresses);
        }

        /// <summary>
        /// Thêm địa chỉ nhận hàng mới
        /// </summary>
        [HttpPost("addresses")]
        public async Task<ActionResult<ShopAddressDto>> AddAddress([FromBody] ShopAddressCreateDto request)
        {
            int customerId = GetCurrentCustomerId();
            var result = await _customerService.AddAddressAsync(customerId, request);
            return Ok(result);
        }

        /// <summary>
        /// Sửa địa chỉ nhận hàng
        /// </summary>
        [HttpPut("addresses/{addressId}")]
        public async Task<ActionResult<ShopAddressDto>> UpdateAddress(int addressId, [FromBody] ShopAddressUpdateDto request)
        {
            int customerId = GetCurrentCustomerId();
            var result = await _customerService.UpdateAddressAsync(customerId, addressId, request);
            return Ok(result);
        }

        /// <summary>
        /// Xóa địa chỉ nhận hàng
        /// </summary>
        [HttpDelete("addresses/{addressId}")]
        public async Task<ActionResult> DeleteAddress(int addressId)
        {
            int customerId = GetCurrentCustomerId();
            await _customerService.DeleteAddressAsync(customerId, addressId);
            return Ok(new { message = "Đã xóa địa chỉ thành công." });
        }

        /// <summary>
        /// Đặt địa chỉ làm mặc định
        /// </summary>
        [HttpPost("addresses/{addressId}/default")]
        public async Task<ActionResult> SetDefaultAddress(int addressId)
        {
            int customerId = GetCurrentCustomerId();
            await _customerService.SetDefaultAddressAsync(customerId, addressId);
            return Ok(new { message = "Đã thiết lập địa chỉ mặc định." });
        }
    }
}
