using backend.DTOs.AiDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers.Shop
{
    [Route("api/shop/ai")]
    [ApiController]
    public class ShopAiController : ControllerBase
    {
        private readonly IGeminiChatService _aiService;

        public ShopAiController(IGeminiChatService aiService)
        {
            _aiService = aiService;
        }

        private int? GetCurrentCustomerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out int id) ? id : null;
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] string? sessionToken)
        {
            int? customerId = GetCurrentCustomerId();
            var sessions = await _aiService.GetCustomerSessionsAsync(customerId, sessionToken);
            return Ok(sessions);
        }

        [HttpGet("sessions/{sessionId}/messages")]
        public async Task<IActionResult> GetSessionMessages(int sessionId, [FromQuery] string? sessionToken)
        {
            int? customerId = GetCurrentCustomerId();
            var messages = await _aiService.GetSessionMessagesAsync(sessionId, customerId, sessionToken);
            return Ok(messages);
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionPayload? payload)
        {
            int? customerId = GetCurrentCustomerId();
            var session = await _aiService.CreateSessionAsync(customerId, payload?.SessionToken, payload?.Title);
            return Ok(session);
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId, [FromQuery] string? sessionToken)
        {
            int? customerId = GetCurrentCustomerId();
            var result = await _aiService.DeleteSessionAsync(sessionId, customerId, sessionToken);
            return Ok(new { success = result });
        }

        [HttpPost("chat")]
        public async Task<IActionResult> SendMessage([FromBody] AiSendMessageRequestDto request)
        {
            try
            {
                int? customerId = GetCurrentCustomerId();
                var response = await _aiService.SendMessageAsync(request, customerId, GetClientIpAddress());
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi xử lý phản hồi từ AI Assistant.", details = ex.Message });
            }
        }

        [HttpPost("confirm-order")]
        public async Task<IActionResult> ConfirmInteractiveOrder([FromBody] ConfirmInteractiveOrderRequestDto request)
        {
            try
            {
                int? customerId = GetCurrentCustomerId();
                var result = await _aiService.ConfirmInteractiveOrderAsync(request, customerId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tạo đơn hàng từ AI Chatbot.", details = ex.Message });
            }
        }
    }

    public class CreateSessionPayload
    {
        public string? SessionToken { get; set; }
        public string? Title { get; set; }
    }
}
