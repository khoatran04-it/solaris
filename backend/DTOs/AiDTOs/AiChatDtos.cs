namespace backend.DTOs.AiDTOs
{
    // --- REQUEST & RESPONSE CHAT ---
    public class AiSendMessageRequestDto
    {
        public int? SessionId { get; set; }
        public string? SessionToken { get; set; }
        public required string Message { get; set; }
    }

    public class AiChatResponseDto
    {
        public int SessionId { get; set; }
        public required string SessionToken { get; set; }
        public string Title { get; set; } = string.Empty;
        public int MessageId { get; set; }
        public required string Content { get; set; }
        public string PayloadType { get; set; } = "none";
        public object? Payload { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // --- SESSION & MESSAGE READ DTOS ---
    public class ChatSessionReadDto
    {
        public int Id { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalMessages { get; set; }
        public string? LastMessage { get; set; }
    }

    public class ChatMessageReadDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public string PayloadType { get; set; } = "none";
        public object? Payload { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // --- INTERACTIVE ORDER / RE-ORDER PAYLOAD ---
    public class InteractiveOrderItemDto
    {
        public int VariantId { get; set; }
        public string VariantCode { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ImagePath { get; set; }
        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class InteractiveOrderPayloadDto
    {
        public string Title { get; set; } = "Đơn Hàng Gợi Ý / Đặt Lại";
        public string? PreviousOrderCode { get; set; }
        public List<InteractiveOrderItemDto> Items { get; set; } = new List<InteractiveOrderItemDto>();
        public decimal SubTotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsFreeShipping { get; set; }
        public decimal FreeShippingThreshold { get; set; } = 300000;
        public string? SuggestedDeliveryAddress { get; set; }
        public string? SuggestedReceiverName { get; set; }
        public string? SuggestedReceiverPhone { get; set; }
    }

    // --- CONFIRM ORDER FROM CHAT ---
    public class ConfirmInteractiveOrderRequestDto
    {
        public int SessionId { get; set; }
        public List<InteractiveOrderItemDto> Items { get; set; } = new List<InteractiveOrderItemDto>();
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        public int? GhnDistrictId { get; set; }
        public string? GhnWardCode { get; set; }
        public decimal ShippingFee { get; set; }
        public int PaymentMethod { get; set; } = 3; // Mặc định 3 = VNPay Sandbox, 1 = COD
        public string? Note { get; set; }
    }

    public class ConfirmInteractiveOrderResponseDto
    {
        public int OrderId { get; set; }
        public required string OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public string? PaymentUrl { get; set; }
        public string? Message { get; set; }
    }

    // --- PRODUCT CARDS PAYLOAD ---
    public class AiProductCardDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public string? ImagePath { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public string UoMName { get; set; } = "Kg";
        public string? Origin { get; set; }
        public string? Certification { get; set; }
        public string? BrixLevel { get; set; }
        public bool IsInStock { get; set; }
    }
}
