using backend.DTOs.AiDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện Service Tích hợp LLM (Google Gemini AI Orchestrator).
    /// Đóng vai trò là "Bộ não" của hệ thống Trợ lý ảo (AI Agent). 
    /// Không chỉ giao tiếp văn bản thuần túy, Service này còn xử lý Function Calling (Tool Use) 
    /// để giúp AI tương tác trực tiếp với Database, tìm kiếm sản phẩm và chốt đơn tự động.
    /// </summary>
    public interface IGeminiChatService
    {
        #region 1. Quản lý Phiên hội thoại (Session Management)
        /// <summary>
        /// Lấy danh sách các phiên hội thoại (Lịch sử Chat) của người dùng để hiển thị Sidebar.
        /// Thiết kế linh hoạt: Hỗ trợ liền mạch giữa Khách vãng lai (dựa vào sessionToken) 
        /// và Khách đã đăng nhập thành viên (customerId).
        /// </summary>
        Task<List<ChatSessionReadDto>> GetCustomerSessionsAsync(int? customerId, string? sessionToken);

        /// <summary>
        /// Lấy toàn bộ lịch sử tin nhắn của một phiên cụ thể.
        /// NGHIỆP VỤ BẢO MẬT: Bắt buộc truyền customerId hoặc sessionToken để xác thực quyền sở hữu 
        /// (Ownership validation), ngăn chặn hành vi xem trộm tin nhắn của người khác.
        /// </summary>
        Task<List<ChatMessageReadDto>> GetSessionMessagesAsync(int sessionId, int? customerId, string? sessionToken);

        /// <summary>
        /// Khởi tạo một phiên hội thoại mới.
        /// Tùy theo logic, Title có thể để rỗng ban đầu và được LLM tự động sinh ra (Summarize) 
        /// sau khi khách hàng hoàn thành 1-2 lượt chat đầu tiên.
        /// </summary>
        Task<ChatSessionReadDto> CreateSessionAsync(int? customerId, string? sessionToken, string? title);

        /// <summary>
        /// Xóa mềm (hoặc ẩn) một phiên hội thoại khỏi giao diện UI của người dùng.
        /// Vẫn giữ lại data dưới DB để phục vụ huấn luyện (Fine-tuning) mô hình AI sau này.
        /// </summary>
        Task<bool> DeleteSessionAsync(int sessionId, int? customerId, string? sessionToken);
        #endregion

        #region 2. Tương tác Trí tuệ nhân tạo (LLM Orchestration)
        /// <summary>
        /// Trái tim của AI Agent: Nhận câu hỏi từ Client, xử lý qua Gemini và trả về kết quả.
        /// LUỒNG THỰC THI LÕI (AGENTIC WORKFLOW):
        /// 1. Tải Lịch sử chat (Context) từ Database.
        /// 2. Gửi Prompt + danh sách Tools (Hàm tìm SP, Hàm check tồn kho) lên Gemini.
        /// 3. Nếu Gemini yêu cầu gọi Tools -> Service tự động query DB -> Trả kết quả ngược lại cho Gemini.
        /// 4. Gemini tổng hợp dữ liệu, sinh ra câu trả lời (Text) kèm theo PayloadJson.
        /// 5. Đóng gói thành AiChatResponseDto và đẩy xuống cho Frontend render UI.
        /// </summary>
        Task<AiChatResponseDto> SendMessageAsync(AiSendMessageRequestDto request, int? customerId, string ipAddress);
        #endregion

        #region 3. Nghiệp vụ Chốt đơn qua Chat (Conversational Commerce)
        /// <summary>
        /// Chuyển đổi "Giỏ hàng ảo" từ Khung Chat thành Đơn hàng thật dưới Database.
        /// ĐƯỢC GỌI KHI: Người dùng bấm nút "Xác nhận Đặt hàng" trên form Interactive Order.
        /// NGHIỆP VỤ LIÊN KẾT: Sẽ gọi chéo sang IOrderService để Reserve Tồn kho, 
        /// IGhnService để tính phí vận chuyển chốt sổ, và IVnPayService (nếu khách thanh toán Online).
        /// </summary>
        Task<ConfirmInteractiveOrderResponseDto> ConfirmInteractiveOrderAsync(ConfirmInteractiveOrderRequestDto request, int? customerId);
        #endregion
    }
}