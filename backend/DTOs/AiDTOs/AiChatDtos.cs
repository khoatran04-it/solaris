using System;
using System.Collections.Generic;

namespace backend.DTOs.AiDTOs
{
    #region 1. Giao tiếp Khách hàng & AI (Request & Response Chat)
    /// <summary>
    /// DTO Client gửi câu hỏi (Prompt) lên cho AI.
    /// Dùng linh hoạt: Nếu có SessionId (đã đăng nhập) hoặc SessionToken (Khách vãng lai) 
    /// thì AI sẽ nối tiếp ngữ cảnh cũ. Nếu null cả 2, AI tự hiểu đây là cuộc hội thoại mới tinh.
    /// </summary>
    public class AiSendMessageRequestDto
    {
        public int? SessionId { get; set; }
        public string? SessionToken { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO AI trả lời lại Client.
    /// Không chỉ chứa văn bản, mà còn đóng gói kèm Dữ liệu UI (Payload) để Frontend 
    /// vẽ ra các Component tương tác (như danh sách sản phẩm hoặc form đặt hàng).
    /// </summary>
    public class AiChatResponseDto
    {
        public int SessionId { get; set; }
        public required string SessionToken { get; set; }
        public string Title { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public required string Content { get; set; }

        /// <summary>Cờ định tuyến UI: "none", "product_cards", "interactive_order", "order_success".</summary>
        public string PayloadType { get; set; } = "none";

        /// <summary>Đối tượng dữ liệu động. Sẽ được parse thành JSON trả về cho Frontend.</summary>
        public object? Payload { get; set; }

        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region 2. Hiển thị Lịch sử Hội thoại (Session & Message Read)
    /// <summary>
    /// DTO Hiển thị danh sách Lịch sử Chat ở thanh Sidebar (Tương tự ChatGPT).
    /// </summary>
    public class ChatSessionReadDto
    {
        public int Id { get; set; }
        public string SessionToken { get; set; } = string.Empty;

        /// <summary>Tiêu đề tóm tắt do LLM tự sinh.</summary>
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalMessages { get; set; }

        /// <summary>Đoạn trích tin nhắn cuối cùng để hiển thị Preview.</summary>
        public string? LastMessage { get; set; }
    }

    /// <summary>
    /// DTO Hiển thị từng bong bóng chat (Bubble) trong màn hình chi tiết.
    /// </summary>
    public class ChatMessageReadDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public string PayloadType { get; set; } = "none";
        public object? Payload { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    #endregion

    #region 3. Giao diện Đặt hàng Tương tác (Interactive Order Payload)
    /// <summary>
    /// DTO Đại diện cho 1 dòng sản phẩm trong Giỏ hàng Ảo do AI tự động tạo ra.
    /// </summary>
    public class InteractiveOrderItemDto
    {
        public int VariantId { get; set; }
        public string VariantCode { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ImagePath { get; set; }
        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        /// <summary>Số lượng AI tự gợi ý dựa trên câu chat của khách (Ví dụ: "Cho tôi 3kg bơ" -> Quantity = 3).</summary>
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        /// <summary>Số lượng khả dụng thực tế trong kho.</summary>
        public decimal AvailableStock { get; set; }

        /// <summary>Thông báo cảnh báo nếu số lượng bị điều chỉnh theo tồn kho khả dụng.</summary>
        public string? WarningMessage { get; set; }
    }

    /// <summary>
    /// DTO Payload cấu trúc Form "Chốt đơn" ngay trong khung chat (Mini-Checkout).
    /// AI AGENTIC WORKFLOW: Nếu khách nói "Mua lại đơn hàng tháng trước", AI sẽ query DB, 
    /// điền sẵn toàn bộ sản phẩm cũ, địa chỉ cũ vào DTO này và đẩy ra cho khách bấm "Xác nhận".
    /// </summary>
    public class InteractiveOrderPayloadDto
    {
        public string Title { get; set; } = "Đơn Hàng Gợi Ý / Đặt Lại";

        /// <summary>Nếu là luồng "Mua lại", đây là mã đơn hàng cũ để tham chiếu.</summary>
        public string? PreviousOrderCode { get; set; }

        public List<InteractiveOrderItemDto> Items { get; set; } = new List<InteractiveOrderItemDto>();

        /// <summary>Cảnh báo tồn kho nếu khách yêu cầu số lượng vượt quá khả dụng thực tế trong kho.</summary>
        public string? StockWarning { get; set; }

        #region Tiền bạc & Upsell Freeship
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsFreeShipping { get; set; }
        public decimal FreeShippingThreshold { get; set; } = 300000;
        #endregion

        #region Gợi ý Địa chỉ (Từ lịch sử của User)
        public string? SuggestedDeliveryAddress { get; set; }
        public string? SuggestedReceiverName { get; set; }
        public string? SuggestedReceiverPhone { get; set; }
        #endregion
    }
    #endregion

    #region 4. Xác nhận Đơn hàng từ Khung Chat (Confirm Order from Chat)
    /// <summary>
    /// DTO Khách hàng bấm nút "Xác nhận Đặt hàng" từ form Interactive Order trong Chat.
    /// Chuyển đổi trạng thái từ Giỏ hàng ảo (Chat) sang Đơn hàng thật (Database).
    /// </summary>
    public class ConfirmInteractiveOrderRequestDto
    {
        /// <summary>Liên kết vào phiên chat để sau khi đặt thành công, AI biết đường trả lời chúc mừng.</summary>
        public int SessionId { get; set; }

        public List<InteractiveOrderItemDto> Items { get; set; } = new List<InteractiveOrderItemDto>();
        public int? CustomerAddressId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        public int? GhnDistrictId { get; set; }
        public string? GhnWardCode { get; set; }
        public decimal ShippingFee { get; set; }

        /// <summary>
        /// Phương thức thanh toán. 
        /// NGHIỆP VỤ MẶC ĐỊNH: 3 = VNPay (Online), 1 = COD. Nếu chọn VNPay, Service sẽ sinh luôn URL thanh toán.
        /// </summary>
        public int PaymentMethod { get; set; } = 3;

        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO Phản hồi sau khi chốt đơn thành công qua Chat.
    /// </summary>
    public class ConfirmInteractiveOrderResponseDto
    {
        public int OrderId { get; set; }
        public required string OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;

        /// <summary>Nếu khách chọn VNPay, Frontend sẽ dùng URL này mở Popup/Tab mới để khách quét QR.</summary>
        public string? PaymentUrl { get; set; }

        /// <summary>Thông báo nghiệp vụ (Ví dụ: "Đã giữ chỗ tồn kho thành công").</summary>
        public string? Message { get; set; }
    }
    #endregion

    #region 5. Giao diện Thẻ Sản phẩm (Product Cards Payload)
    /// <summary>
    /// DTO Dữ liệu hiển thị Thẻ sản phẩm (Product Card).
    /// AI FUNCTION CALLING: Khi khách hỏi "Có dưa lưới loại nào ngọt không?", AI sẽ gọi hàm, 
    /// Backend Query DB tìm dưa lưới, map ra danh sách DTO này và yêu cầu Frontend vẽ thành dạng Swiper/Carousel.
    /// </summary>
    public class AiProductCardDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public required string Name { get; set; }
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryGroupName { get; set; }
        public required string Slug { get; set; }
        public string? ImagePath { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public string UoMName { get; set; } = "Kg";

        /// <summary>Nguồn gốc xuất xứ (Yếu tố chốt sale quan trọng của ngành nông sản).</summary>
        public string? Origin { get; set; }

        /// <summary>Chứng nhận tiêu chuẩn (VietGAP, GlobalGAP, Organic).</summary>
        public string? Certification { get; set; }

        /// <summary>Độ Brix (Độ ngọt). Rất quan trọng khi AI tư vấn trái cây (Ví dụ: "Dưa lưới này độ ngọt lên tới 14 Brix").</summary>
        public string? BrixLevel { get; set; }

        /// <summary>Cờ báo hiệu còn hàng hay không, giúp Frontend disable nút "Thêm vào giỏ" nếu hết hàng.</summary>
        public bool IsInStock { get; set; }
    }
    #endregion

    #region 6. Tra cứu Đơn hàng (Order Tracking Payload)
    public class AiOrderTrackingItemDto
    {
        public string VariantName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UoMName { get; set; } = "Kg";
        public decimal TotalPrice { get; set; }
    }

    public class AiOrderTrackingDto
    {
        public int OrderId { get; set; }
        public required string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int PaymentStatus { get; set; }
        public string PaymentStatusName { get; set; } = string.Empty;
        public int PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? ShippingProvider { get; set; }
        public List<AiOrderTrackingItemDto> Items { get; set; } = new List<AiOrderTrackingItemDto>();
    }
    #endregion
}