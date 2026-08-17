using backend.Models.Enums;

namespace backend.Models
{
    public class Order : ISoftDelete
    {
        public int Id { get; set; }

        public required string OrderCode { get; set; }

        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int? CustomerAddressId { get; set; }
        public virtual CustomerAddress? CustomerAddress { get; set; }

        // Snapshot địa chỉ tại thời điểm đặt (phòng trường hợp khách sửa/xóa địa chỉ sau này)
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        // Kho được chỉ định xuất hàng (Tính toán tự động theo khoảng cách hoặc thủ công)
        public int? WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        public decimal SubTotal { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingFee { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation
        public virtual ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public virtual ICollection<InventoryIssue> InventoryIssues { get; set; } = new List<InventoryIssue>();
        public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();
    }
}
