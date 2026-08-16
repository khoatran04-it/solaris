namespace backend.Models.Enums
{
    public enum InventoryReceiptStatus
    {
        Pending = 1,            // Chờ nhập kho (Phiếu vừa tạo, chờ xe tải tới)
        Inspecting = 2,         // Đang kiểm đếm (Bốc hàng, cân đo, ghi nhận SL thực tế)
        Completed = 3,          // Hoàn tất (Đã duyệt nhập kho, tồn kho đã được cộng)
        Cancelled = 4           // Đã hủy (Lập sai / Hàng bị từ chối toàn bộ)
    }
}
