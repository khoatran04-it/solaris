using backend.DTOs.AuthDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Bắt đúng lỗi sai User/Pass hoặc bị khóa để trả về 401 Unauthorized
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Các lỗi khác trả về 400 Bad Request
                return BadRequest(new { message = "Đã xảy ra lỗi trong quá trình đăng nhập.", details = ex.Message });
            }
        }
    }
}