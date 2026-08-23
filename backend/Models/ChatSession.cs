namespace backend.Models
{
    /// <summary>
    /// Phiên hội thoại của khách hàng với AI Chatbot Assistant.
    /// Hỗ trợ cả khách vãng lai (SessionToken) và khách đã đăng nhập (CustomerId).
    /// </summary>
    public class ChatSession
    {
        public int Id { get; set; }

        public int? CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        /// <summary>Định danh phiên trên trình duyệt cho khách vãng lai hoặc liên kết thiết bị</summary>
        public string SessionToken { get; set; } = string.Empty;

        /// <summary>Tiêu đề tóm tắt do AI sinh ra (ví dụ: "Tư vấn bơ 034 & Đặt hàng")</summary>
        public string Title { get; set; } = "Cuộc trò chuyện mới";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
