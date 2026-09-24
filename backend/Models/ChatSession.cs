using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Phiên hội thoại (Chat Session) giữa Khách hàng và AI Chatbot Assistant.
    /// Đóng vai trò là "Ngữ cảnh" (Context) để AI ghi nhớ mạch trò chuyện. 
    /// Thiết kế thông minh: Hỗ trợ cả khách vãng lai (Guest) và khách đã đăng nhập, 
    /// cho phép đồng bộ hóa dữ liệu nếu khách hàng quyết định đăng nhập giữa chừng.
    /// </summary>
    public class ChatSession
    {
        #region Định danh & Trạng thái (Identity & State)
        public int Id { get; set; }

        /// <summary>
        /// Tiêu đề tóm tắt nội dung cuộc trò chuyện.
        /// NGHIỆP VỤ UX: Thường được tự động sinh ra bởi AI (LLM) sau vài lượt chat đầu tiên 
        /// (Ví dụ: "Tư vấn bơ 034 & Đặt hàng") để giúp người dùng dễ dàng tìm lại lịch sử chat.
        /// </summary>
        public string Title { get; set; } = "Cuộc trò chuyện mới";

        /// <summary>
        /// Trạng thái của phiên hội thoại.
        /// Khi khách hàng tự xóa chat hoặc phiên đã quá cũ, hệ thống chuyển IsActive = false (Soft-close) 
        /// để làm sạch giao diện UI nhưng vẫn giữ lại data để huấn luyện AI (Fine-tuning) sau này.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Khối dữ liệu JSON lưu trữ Giỏ hàng hội thoại tạm thời (Conversational Draft Order State).
        /// Cho phép cộng dồn, sửa đổi, xóa món xuyên suốt nhiều lượt chat trong cùng một phiên hội thoại.
        /// </summary>
        public string? DraftOrderJson { get; set; }
        #endregion

        #region Liên kết Người dùng (User Association)
        /// <summary>
        /// Mã định danh Khách hàng (Nếu khách đã đăng nhập).
        /// </summary>
        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        /// <summary>
        /// Định danh phiên ẩn danh (Guest Session).
        /// NGHIỆP VỤ THEO DÕI: Được lưu trữ dưới dạng Cookie hoặc LocalStorage trên trình duyệt của khách.
        /// Nếu khách vãng lai chat với AI, sau đó quyết định "Đăng nhập", hệ thống sẽ dùng SessionToken này 
        /// để gán CustomerId vào phiên, giúp khách không bị mất mạch trò chuyện.
        /// </summary>
        public string SessionToken { get; set; } = string.Empty;
        #endregion

        #region Thời gian & Theo dõi (Timestamps)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời điểm có tin nhắn (Message) mới nhất.
        /// Dùng để sắp xếp danh sách lịch sử chat trên App/Web sao cho phiên nào mới chat sẽ nổi lên đầu.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        #endregion

        #region Liên kết Chi tiết (Messages)
        /// <summary>
        /// Danh sách toàn bộ các tin nhắn (Prompt của người dùng & Response của AI) thuộc phiên này.
        /// Khi gọi API AI (như OpenAI/Gemini), hệ thống sẽ truy xuất danh sách này để bơm vào "Context History".
        /// </summary>
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        #endregion
    }
}