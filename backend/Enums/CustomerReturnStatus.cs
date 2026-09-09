namespace backend.Models.Enums
{
    public enum CustomerReturnStatus
    {
        Pending = 1,        // Chờ tiếp nhận (Khách gửi yêu cầu / Tự động sinh khi giao thất bại)
        Approved = 2,       // Đã duyệt (Chờ điều phối xe thu hồi)
        PickingUp = 6,      // Đang thu hồi (Tài xế đang đi lấy hàng từ khách)
        Inspecting = 3,     // Đã về kho - Đang kiểm định QC / Đang xử lý nhập kho
        Completed = 4,      // Hoàn tất (Đã nhập kho thu hồi & hoàn tiền)
        Rejected = 5        // Từ chối nhận trả hàng
    }
}
