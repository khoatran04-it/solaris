namespace backend.Models
{
    /// <summary>
    /// Dòng chi tiết mặt hàng và phân loại tình trạng hàng trả lại từ khách.
    /// </summary>
    public class CustomerReturnDetail
    {
        public int Id { get; set; }

        public int CustomerReturnId { get; set; }
        public virtual CustomerReturn? CustomerReturn { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Lô hàng nông sản được khách trả lại</summary>
        public int BatchId { get; set; }
        public virtual ProductBatch? Batch { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Tổng số lượng khách mang trả</summary>
        public decimal ReturnedQuantity { get; set; }

        /// <summary>Số lượng còn nguyên vẹn đạt chuẩn QC $\to$ Nhập lại QuantityAvailable</summary>
        public decimal AcceptedQuantity { get; set; } = 0;

        /// <summary>Số lượng móp méo / hỏng hóc $\to$ Nhập vào QuantityDamaged</summary>
        public decimal DamagedQuantity { get; set; } = 0;

        /// <summary>Đơn giá tính toán hoàn tiền</summary>
        public decimal UnitPrice { get; set; } = 0;

        /// <summary>Thành tiền hoàn = AcceptedQuantity * UnitPrice</summary>
        public decimal RefundAmount { get; set; } = 0;

        /// <summary>Lý do từ chối hoặc ghi chú tình trạng lỗi</summary>
        public string? RejectReason { get; set; }
    }
}
