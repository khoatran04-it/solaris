namespace backend.Models.Enums
{
    public enum CustomerReturnStatus
    {
        Pending = 1,        // Chờ nhận hàng (Khách yêu cầu trả hàng)
        Inspecting = 2,     // Đang kiểm định QC (Hàng đang nằm ở QuantityQC)
        Completed = 3,      // Hoàn tất (Đã phân loại vào Available/Damaged và hoàn tiền)
        Rejected = 4        // Từ chối nhận trả hàng
    }
}
