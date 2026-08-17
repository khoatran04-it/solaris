namespace backend.Models.Enums
{
    public enum InventoryAdjustmentStatus
    {
        Draft = 1,
        Approved = 2,
        Cancelled = 3
    }

    public enum InventoryAdjustmentReason
    {
        Surplus = 1,        // Thừa kiểm kê
        LossTheft = 2,      // Mất mát / Thất thoát
        Spoilage = 3,       // Hư hỏng do biến đổi sinh hóa / nhiệt độ
        Damage = 4,         // Dập nát bao bì / vỡ bể khi bốc dỡ
        Expiry = 5,         // Hết hạn sử dụng
        Shrinkage = 6,      // Hao hụt tự nhiên
        DataCorrection = 7  // Sai lệch nhập liệu
    }

    public enum InventoryAdjustmentType
    {
        IncreaseAvailable = 1,  // Tăng tồn kho khả dụng (+)
        DecreaseAvailable = 2,  // Giảm tồn kho khả dụng (-)
        MoveToDamaged = 3,      // Chuyển từ khả dụng sang hàng hỏng (Available -> Damaged)
        DisposeDamaged = 4      // Xuất hủy hàng hỏng khỏi kho (- Damaged)
    }
}
