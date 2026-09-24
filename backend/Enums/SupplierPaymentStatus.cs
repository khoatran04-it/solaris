namespace backend.Models.Enums
{
    /// <summary>
    /// Trạng thái thanh toán công nợ cho Đơn đặt mua hàng (Purchase Order).
    /// </summary>
    public enum SupplierPaymentStatus
    {
        /// <summary>Chưa thanh toán (0 VND).</summary>
        Unpaid = 1,

        /// <summary>Đã thanh toán một phần (đặt cọc hoặc trả dần từng đợt).</summary>
        PartiallyPaid = 2,

        /// <summary>Đã tất toán toàn bộ công nợ cho lô hàng thực nhận.</summary>
        Paid = 3,

        /// <summary>Quá hạn thanh toán theo điều khoản hợp đồng/hạn nợ.</summary>
        Overdue = 4
    }
}
