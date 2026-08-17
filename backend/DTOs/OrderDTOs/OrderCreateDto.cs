using backend.Models.Enums;

namespace backend.DTOs.OrderDTOs
{
    public class OrderCreateDto
    {
        public int CustomerId { get; set; }
        public int? CustomerAddressId { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        // Có thể truyền kho cụ thể hoặc để trống để hệ thống tự động định tuyến
        public int? WarehouseId { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public decimal ShippingFee { get; set; } = 0;
        public string? Note { get; set; }

        public List<OrderDetailCreateDto> Details { get; set; } = new();
    }

    public class OrderDetailCreateDto
    {
        public int VariantId { get; set; }
        public int UoMId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; } // Nếu null, Service tự lấy từ VariantPrice
        public decimal DiscountAmount { get; set; } = 0;
    }
}
