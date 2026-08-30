using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Tin nhắn trong phiên hội thoại AI (Chat Message).
    /// Trái tim của hệ thống Trợ lý ảo (AI Agent) B2C. Không chỉ lưu trữ văn bản (Text/Markdown) thông thường, 
    /// thiết kế này cực kỳ hiện đại khi hỗ trợ lưu trữ Payload có cấu trúc, cho phép biến khung chat thành 
    /// một trải nghiệm mua sắm thu nhỏ (Mini-app) với các nút bấm và form tương tác.
    /// </summary>
    public class ChatMessage
    {
        #region Định danh & Liên kết (Identity & Relation)
        public int Id { get; set; }

        /// <summary>
        /// Mã định danh của Phiên hội thoại.
        /// Khi truy vấn gửi lên các LLM (như OpenAI, Gemini), hệ thống sẽ Select toàn bộ Message 
        /// có cùng SessionId để tạo thành Ngữ cảnh (Context History) giúp AI hiểu được mạch nói chuyện.
        /// </summary>
        public int SessionId { get; set; }
        public virtual ChatSession? Session { get; set; }
        #endregion

        #region Vai trò & Nội dung (Role & Content)
        /// <summary>
        /// Vai trò của người gửi tin nhắn (Tương thích chuẩn API của các LLM hiện nay).
        /// - "user": Khách hàng gõ câu hỏi.
        /// - "model" / "assistant": Phản hồi từ AI.
        /// - "system": Các câu lệnh mồi (System Prompts) định hướng hành vi của AI, thường bị ẩn trên UI.
        /// </summary>
        public string Role { get; set; } = "user";

        /// <summary>
        /// Nội dung tin nhắn dạng văn bản (Hỗ trợ Markdown như in đậm, danh sách, bảng biểu).
        /// Nếu tin nhắn chủ yếu để render thẻ sản phẩm (Payload), trường này có thể dùng làm câu dẫn 
        /// (Ví dụ: "Dạ, em tìm thấy các loại bơ 034 đang có sẵn tại cửa hàng đây ạ:").
        /// </summary>
        public string Content { get; set; } = string.Empty;
        #endregion

        #region Dữ liệu Tương tác nâng cao (Rich UI Payload / Agentic Tool)
        /// <summary>
        /// Cờ định tuyến Giao diện (UI Routing Flag) cho Frontend.
        /// - "none": Hiển thị Content như một khung chat bong bóng (Chat bubble) bình thường.
        /// - "product_cards": Yêu cầu Frontend render một Carousel vuốt ngang chứa danh sách sản phẩm.
        /// - "interactive_order": Yêu cầu Frontend render một Form mini-checkout để khách điền địa chỉ.
        /// - "order_success": Yêu cầu Frontend render Biên lai điện tử (E-Receipt) chúc mừng đặt hàng thành công.
        /// </summary>
        public string PayloadType { get; set; } = "none";

        /// <summary>
        /// Khối dữ liệu JSON thô chứa thông số cho UI Component.
        /// NGHIỆP VỤ FUNCTION CALLING: Khi AI quyết định cần gọi hàm (Ví dụ: Tìm sản phẩm), Backend sẽ chạy truy vấn SQL, 
        /// đóng gói kết quả (Tên, Giá, Ảnh) thành JSON và lưu vào đây. Frontend sẽ parse chuỗi JSON này 
        /// để bóc tách dữ liệu và vẽ lên các thẻ tương tác cực đẹp mắt.
        /// </summary>
        public string? PayloadJson { get; set; }
        #endregion

        #region Thời gian (Timestamp)
        /// <summary>
        /// Dấu thời gian tin nhắn được tạo vào DB.
        /// Sống còn để sắp xếp lịch sử hội thoại (Order by CreatedAt ASC) trước khi bơm vào Prompt. 
        /// Nếu sai thứ tự, AI sẽ bị "lú" và trả lời ngớ ngẩn.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        #endregion
    }
}