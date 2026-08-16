namespace backend.Models.Enums
{
    public enum TransactionType
    {
        // --- LUỒNG VÀO (INBOUND) ---
        Receipt = 1,          // Nhập kho từ NCC (Tăng Available)
        CustomerReturn = 2,   // Khách hoàn trả (Tăng QC)
        TransferIn = 3,       // Nhận hàng từ kho khác (Tăng Available)

        // --- LUỒNG RA (OUTBOUND) ---
        Issue = 4,            // Xuất bán cho khách (Trừ Reserved)
        SupplierReturn = 5,   // Xuất trả NCC (Trừ Available hoặc Damaged)
        TransferOut = 6,      // Xuất chuyển sang kho khác (Trừ Available)

        // --- LUỒNG NỘI BỘ (INTERNAL) ---
        Reserve = 7,          // Giữ chỗ đơn hàng (Trừ Available, Tăng Reserved)
        Unreserve = 8,        // Hủy giữ chỗ do khách hủy đơn (Trừ Reserved, Tăng Available)
        Adjustment = 9        // Điều chỉnh kiểm kê, xử lý hàng QC (Tăng/Giảm tùy loại)
    }
}