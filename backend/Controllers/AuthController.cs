using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    /// <summary>
    /// API điều khiển xác thực danh tính và quản lý phiên đăng nhập (Authentication).
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Đăng nhập hệ thống bằng tài khoản và mật khẩu.
        /// </summary>
        /// <param name="request">Thông tin tên đăng nhập và mật khẩu.</param>
        /// <returns>JWT Token và thông tin phân quyền người dùng.</returns>
        /// <response code="200">Đăng nhập thành công, trả về token và thông tin tài khoản.</response>
        /// <response code="401">Tên đăng nhập, mật khẩu không đúng hoặc tài khoản đã bị vô hiệu hóa.</response>
        /// <response code="400">Yêu cầu không hợp lệ hoặc lỗi hệ thống phát sinh.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Đã xảy ra lỗi trong quá trình xử lý đăng nhập.",
                    details = ex.Message
                });
            }
        }
    }
}