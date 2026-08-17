using backend.Models.Enums;

namespace backend.DTOs.OrderDTOs
{
    public class OrderUpdateDto
    {
        public OrderStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public int? WarehouseId { get; set; }
        public string? Note { get; set; }
        public string? CancellationReason { get; set; }
    }
}
