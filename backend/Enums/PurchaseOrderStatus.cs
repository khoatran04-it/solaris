namespace backend.Models.Enums
{
    public enum PurchaseOrderStatus
    {
        Draft = 1,              // Bản nháp (NV mới lên đơn, chưa gửi NCC)
        Processing = 2,         // Đang xử lý (Đã gửi NCC, chờ xác nhận)
        Approved = 3,           // Đã duyệt (NCC đồng ý, sẵn sàng tạo Phiếu Nhập)
        PartiallyReceived = 4,  // Đã nhận một phần (NCC giao từng đợt)
        Completed = 5,          // Hoàn tất (Đã nhận đủ hàng hoặc chủ động đóng PO)
        Cancelled = 6           // Đã hủy (NCC từ chối / Hết nhu cầu)
    }
}
