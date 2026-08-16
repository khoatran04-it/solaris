using backend.Models.Enums;

namespace backend.DTOs.PurchaseOrderDTOs
{
    public class PurchaseOrderUpdateDto
    {
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Note { get; set; }
        public PurchaseOrderStatus Status { get; set; }
        public string? CancellationReason { get; set; }
    }
}
