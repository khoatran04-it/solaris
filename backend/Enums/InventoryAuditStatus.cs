namespace backend.Models.Enums
{
    public enum InventoryAuditStatus
    {
        Draft = 1,
        InProgress = 2,
        PendingApproval = 3,
        Completed = 4,
        Cancelled = 5
    }

    public enum InventoryAuditType
    {
        Full = 1,       // Kiểm kê toàn bộ
        Cycle = 2,      // Kiểm kê cuốn chiếu (danh mục / lô)
        Spot = 3        // Kiểm kê đột xuất
    }
}
