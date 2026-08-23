namespace backend.Models
{
    /// <summary>
    /// Từng tin nhắn trao đổi trong phiên hội thoại AI.
    /// Hỗ trợ cả text markdown và payload có cấu trúc (thẻ sản phẩm, thẻ đơn hàng tương tác, kết quả thanh toán).
    /// </summary>
    public class ChatMessage
    {
        public int Id { get; set; }

        public int SessionId { get; set; }
        public virtual ChatSession? Session { get; set; }

        /// <summary>Role: "user" | "model" | "system"</summary>
        public string Role { get; set; } = "user";

        /// <summary>Nội dung tin nhắn dạng văn bản (hỗ trợ Markdown)</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Loại nội dung đặc biệt: "none" | "product_cards" | "interactive_order" | "order_success"</summary>
        public string PayloadType { get; set; } = "none";

        /// <summary>Dữ liệu JSON kèm theo để frontend render thành các thẻ tương tác</summary>
        public string? PayloadJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
