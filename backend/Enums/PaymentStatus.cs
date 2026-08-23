namespace backend.Models.Enums
{
    public enum PaymentStatus
    {
        Unpaid = 1,         // Chưa thanh toán
        PartiallyPaid = 2,  // Đã đặt cọc / Thanh toán 1 phần
        Paid = 3,           // Đã thanh toán đủ 100%
        Refunded = 4,       // Đã hoàn tiền (khi hủy đơn hoặc trả hàng)
        Failed = 5          // Thanh toán thất bại / Giao dịch lỗi
    }
}
