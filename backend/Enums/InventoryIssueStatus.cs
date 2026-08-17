namespace backend.Models.Enums
{
    public enum InventoryIssueStatus
    {
        Pending = 1,        // Chờ xử lý (Phiếu xuất mới tạo)
        Picking = 2,        // Đang gom hàng / Đang bốc dỡ theo Lô
        Completed = 3,      // Đã xuất kho thành công (Trừ Reserved, ghi sổ cái Issue)
        Cancelled = 4       // Đã hủy phiếu xuất
    }
}
