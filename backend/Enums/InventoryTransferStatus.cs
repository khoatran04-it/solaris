namespace backend.Models.Enums
{
    public enum InventoryTransferStatus
    {
        Draft = 1,          // Bản nháp lệnh chuyển
        Approved = 5,       // Đã duyệt (Sẵn sàng điều phối xe vận chuyển / xuất kho)
        InTransit = 2,      // Đang vận chuyển (Đã rời kho A -> Trừ kho A, ghi TransferOut)
        Completed = 3,      // Đã nhập kho đích (Đã tới kho B -> Cộng kho B, ghi TransferIn)
        Cancelled = 4       // Đã hủy (Hoàn trả lại kho A)
    }
}
