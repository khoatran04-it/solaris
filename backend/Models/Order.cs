using backend.Models.Enums;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Đơn Bán Hàng Của Khách Hàng (Customer Sales Order).
    /// Tích hợp Thuật toán Định Tuyến Kho Thông Minh (Smart Routing) và Giữ Chỗ Tồn Kho (Stock Reservation).
    /// </summary>
    public class Order : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã đơn hàng (Ví dụ: ORD-20260817-001)</summary>
        public required string OrderCode { get; set; }

        /// <summary>Khách hàng đặt mua</summary>
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        /// <summary>ID Địa chỉ nhận hàng</summary>
        public int? CustomerAddressId { get; set; }
        public virtual CustomerAddress? CustomerAddress { get; set; }

        // --- SNAPSHOT ĐỊA CHỈ TẠI THỜI ĐIỂM ĐẶT ---
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public string? DeliveryAddress { get; set; }

        /// <summary>Kho xuất hàng (được tính toán tự động theo khoảng cách GPS gần nhất hoặc chỉ định thủ công)</summary>
        public int? WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Trạng thái đơn hàng: Chờ xác nhận, Đã duyệt (giữ chỗ), Đang đóng gói, Đang giao, Đã giao, Đã hủy</summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>Trạng thái thanh toán: Chưa thanh toán, Đã thanh toán, Đã hoàn tiền...</summary>
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        /// <summary>Phương thức thanh toán: COD, Chuyển khoản, Thẻ tín dụng...</summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        /// <summary>Tiền hàng trước giảm giá</summary>
        public decimal SubTotal { get; set; } = 0;

        /// <summary>Số tiền giảm giá (từ Hạng thành viên CustomerTier + Mã khuyến mãi)</summary>
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>Phí vận chuyển giao hàng</summary>
        public decimal ShippingFee { get; set; } = 0;

        /// <summary>Tổng tiền khách phải thanh toán = SubTotal - DiscountAmount + ShippingFee</summary>
        public decimal TotalAmount { get; set; } = 0;

        public string? Note { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public virtual ICollection<InventoryIssue> InventoryIssues { get; set; } = new List<InventoryIssue>();
        public virtual ICollection<CustomerReturn> CustomerReturns { get; set; } = new List<CustomerReturn>();
    }
}
