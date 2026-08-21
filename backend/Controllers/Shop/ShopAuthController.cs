using backend.DTOs.ShopDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/auth")]
    public class ShopAuthController : ShopBaseController
    {
        private readonly IShopAuthService _authService;

        public ShopAuthController(IShopAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng ký tài khoản khách hàng mới
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ShopAuthResponseDto>> Register([FromBody] ShopRegisterRequestDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
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
        /// Đăng nhập tài khoản khách hàng
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ShopAuthResponseDto>> Login([FromBody] ShopLoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin khách hàng đang đăng nhập
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ShopCustomerInfoDto>> GetCurrentUser()
        {
            int customerId = GetCurrentCustomerId();
            var result = await _authService.GetCurrentCustomerAsync(customerId);
            return Ok(result);
        }
    }
}
