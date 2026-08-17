namespace backend.Models.Enums
{
    public enum OrderStatus
    {
        Draft = 1,          // Đơn nháp
        Pending = 2,        // Chờ xác nhận (Khách vừa đặt)
        Confirmed = 3,      // Đã xác nhận / Đã khóa giữ chỗ tồn kho (Reserved)
        Processing = 4,     // Đang chuẩn bị hàng / Đang đóng gói (Picking & Packing)
        Shipping = 5,       // Đang giao hàng (Đã xuất kho)
        Completed = 6,      // Giao thành công / Hoàn tất đơn hàng
        Cancelled = 7       // Đã hủy đơn (Trả lại tồn kho khả dụng)
    }
}
