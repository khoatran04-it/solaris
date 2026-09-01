namespace backend.Models.Enums
{
    public enum CustomerReturnStatus
    {
        Pending = 1,        // Chờ tiếp nhận (Khách gửi yêu cầu)
        Approved = 2,       // Đã duyệt (Chờ nhận hàng tại kho)
        Inspecting = 3,     // Đang kiểm định QC / Đang xử lý nhập kho
        Completed = 4,      // Hoàn tất (Đã nhập kho thu hồi & hoàn tiền)
        Rejected = 5        // Từ chối nhận trả hàng
    }
}
