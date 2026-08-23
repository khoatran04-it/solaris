using backend.DTOs.AiDTOs;

namespace backend.Services.Interfaces
{
    public interface IGeminiChatService
    {
        Task<List<ChatSessionReadDto>> GetCustomerSessionsAsync(int? customerId, string? sessionToken);
        Task<List<ChatMessageReadDto>> GetSessionMessagesAsync(int sessionId, int? customerId, string? sessionToken);
        Task<ChatSessionReadDto> CreateSessionAsync(int? customerId, string? sessionToken, string? title);
        Task<bool> DeleteSessionAsync(int sessionId, int? customerId, string? sessionToken);
        Task<AiChatResponseDto> SendMessageAsync(AiSendMessageRequestDto request, int? customerId, string ipAddress);
        Task<ConfirmInteractiveOrderResponseDto> ConfirmInteractiveOrderAsync(ConfirmInteractiveOrderRequestDto request, int? customerId);
    }
}
