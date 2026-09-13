using backend.DTOs.AiDTOs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace backend.Controllers.Shop
{
    /// <summary>
    /// Controller Quản lý Trợ lý Ảo AI B2C (Solaris AI Chatbot & Conversational Commerce).
    /// Hỗ trợ luồng đàm thoại thông minh, đề xuất sản phẩm thời gian thực, chốt đơn trực tiếp trong khung chat và quản lý lịch sử hội thoại.
    /// Khóa bảo vệ nghiêm ngặt: Yêu cầu đăng nhập tài khoản để sử dụng Trợ lý AI và chốt đơn.
    /// </summary>
    [Authorize]
    [Route("api/shop/ai")]
    [ApiController]
    [Produces("application/json")]
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

        /// <summary>
        /// Lấy danh sách các phiên hội thoại (Chat History) của khách hàng.
        /// </summary>
        /// <param name="sessionToken">Mã token định danh của khách vãng lai (nếu chưa đăng nhập).</param>
        /// <returns>Danh sách các phiên trò chuyện sắp xếp theo thời gian mới nhất.</returns>
        /// <response code="200">Truy vấn danh sách phiên hội thoại thành công.</response>
        [AllowAnonymous]
        [HttpGet("sessions")]
        [ProducesResponseType(typeof(List<ChatSessionReadDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSessions([FromQuery] string? sessionToken)
        {
            int? customerId = GetCurrentCustomerId();
            var sessions = await _aiService.GetCustomerSessionsAsync(customerId, sessionToken);
            return Ok(sessions);
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử tin nhắn trong một phiên hội thoại cụ thể.
        /// </summary>
        /// <param name="sessionId">Mã định danh của phiên hội thoại.</param>
        /// <param name="sessionToken">Token của khách vãng lai để xác thực quyền truy cập.</param>
        /// <returns>Danh sách tin nhắn (User prompts và AI replies kèm rich payloads).</returns>
        /// <response code="200">Truy vấn lịch sử tin nhắn thành công.</response>
        /// <response code="404">Không tìm thấy phiên hội thoại.</response>
        [AllowAnonymous]
        [HttpGet("sessions/{sessionId}/messages")]
        [ProducesResponseType(typeof(List<ChatMessageReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSessionMessages(int sessionId, [FromQuery] string? sessionToken)
        {
            int? customerId = GetCurrentCustomerId();
            var messages = await _aiService.GetSessionMessagesAsync(sessionId, customerId, sessionToken);
            return Ok(messages);
        }

        /// <summary>
        /// Khởi tạo một phiên hội thoại mới với AI Assistant.
        /// </summary>
        /// <param name="payload">Thông tin token và tiêu đề phiên (tùy chọn).</param>
        /// <returns>Thông tin phiên hội thoại mới tạo.</returns>
        /// <response code="200">Khởi tạo phiên hội thoại thành công.</response>
        [AllowAnonymous]
        [HttpPost("sessions")]
        [ProducesResponseType(typeof(ChatSessionReadDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionPayload? payload)
        {
            int? customerId = GetCurrentCustomerId();
            var session = await _aiService.CreateSessionAsync(customerId, payload?.SessionToken, payload?.Title);
            return Ok(session);
        }

        /// <summary>
        /// Xóa mềm một phiên hội thoại khỏi giao diện người dùng.
        /// </summary>
        /// <param name="sessionId">Mã định danh của phiên cần xóa.</param>
        /// <param name="sessionToken">Token xác thực phiên.</param>
        /// <returns>Trạng thái thành công của thao tác xóa.</returns>
        /// <response code="200">Xóa phiên hội thoại thành công.</response>
        /// <response code="404">Không tìm thấy phiên hội thoại để xóa.</response>
        [AllowAnonymous]
        [HttpDelete("sessions/{sessionId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSession(int sessionId, [FromQuery] string? sessionToken)
        {
            int? customerId = GetCurrentCustomerId();
            var result = await _aiService.DeleteSessionAsync(sessionId, customerId, sessionToken);
            if (!result)
            {
                return NotFound(new { message = "Không tìm thấy phiên hội thoại hoặc bạn không có quyền xóa." });
            }
            return Ok(new { success = true, message = "Đã xóa phiên hội thoại thành công." });
        }

        /// <summary>
        /// Gửi tin nhắn câu hỏi (Prompt) lên AI Assistant và nhận phản hồi kèm UI Payload.
        /// </summary>
        /// <param name="request">Thông tin câu hỏi và ngữ cảnh phiên chat.</param>
        /// <returns>Phản hồi từ AI (Văn bản + Rich UI Payload).</returns>
        /// <response code="200">Xử lý câu hỏi và phản hồi từ AI thành công.</response>
        /// <response code="400">Yêu cầu không hợp lệ hoặc lỗi trong quá trình phân tích ý định.</response>
        [AllowAnonymous]
        [HttpPost("chat")]
        [ProducesResponseType(typeof(AiChatResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendMessage([FromBody] AiSendMessageRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });
            }

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

        /// <summary>
        /// Xác nhận đặt hàng trực tiếp từ khung chat (Conversational Checkout).
        /// </summary>
        /// <param name="request">Dữ liệu đơn hàng do AI tạo ra và khách hàng đã xác nhận.</param>
        /// <returns>Thông tin đơn hàng thực tế đã được tạo trong Database và URL thanh toán (nếu chọn VNPay).</returns>
        /// <response code="200">Chốt đơn hàng từ chat thành công.</response>
        /// <response code="400">Dữ liệu đơn hàng không hợp lệ.</response>
        [HttpPost("confirm-order")]
        [ProducesResponseType(typeof(ConfirmInteractiveOrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi tạo đơn hàng từ AI Chatbot.", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Payload khởi tạo phiên hội thoại mới.
    /// </summary>
    public class CreateSessionPayload
    {
        /// <summary>Token phiên ẩn danh (nếu có).</summary>
        public string? SessionToken { get; set; }

        /// <summary>Tiêu đề tùy chỉnh cho phiên.</summary>
        public string? Title { get; set; }
    }
}
