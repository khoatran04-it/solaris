using backend.Models.Enums;

namespace backend.DTOs.ShopDTOs
{
    public class ShopCheckoutRequestDto
    {
        // Có thể chọn từ Sổ địa chỉ đã lưu hoặc nhập trực tiếp
        public int? CustomerAddressId { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? StreetAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public string? Note { get; set; }
    }

    public class ShopOrderItemDto
    {
        public int DetailId { get; set; }
        public int VariantId { get; set; }
        public required string VariantName { get; set; }
        public required string VariantCode { get; set; }
        public string? ImagePath { get; set; }
        public int UoMId { get; set; }
        public required string UoMName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal IssuedQuantity { get; set; }
    }

    public class ShopOrderReadDto
    {
        public int Id { get; set; }
        public required string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }

        public OrderStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;

        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusName { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public List<ShopOrderItemDto> Items { get; set; } = new List<ShopOrderItemDto>();
    }

    public class ShopOrderCancelRequestDto
    {
        public required string Reason { get; set; }
    }
}
