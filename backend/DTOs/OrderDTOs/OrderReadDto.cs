using backend.Models.Enums;

namespace backend.DTOs.OrderDTOs
{
    public class OrderReadDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        public int? CustomerAddressId { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime OrderDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<OrderDetailReadDto> Details { get; set; } = new();
    }

    public class OrderDetailReadDto
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;

        public int UoMId { get; set; }
        public string UoMName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal IssuedQuantity { get; set; }
    }
}
