namespace backend.Models
{
    /// <summary>
    /// Dòng chi tiết mặt hàng trong Đơn Bán Hàng (Sales Order Detail).
    /// </summary>
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Đơn vị tính khách chọn mua (Kg, Hộp, Thùng...)</summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Số lượng khách đặt theo UoM</summary>
        public decimal Quantity { get; set; }

        /// <summary>Số lượng đã quy đổi về Đơn vị tính cơ sở (Base UoM) phục vụ Giữ chỗ (Reserve) và Trừ kho</summary>
        public decimal BaseQuantity { get; set; }

        /// <summary>Đơn giá bán tại thời điểm đặt (VND)</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Số tiền giảm giá trên dòng sản phẩm</summary>
        public decimal DiscountAmount { get; set; } = 0;

        /// <summary>Thành tiền = (Quantity * UnitPrice) - DiscountAmount</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>Tiến độ xuất kho lũy kế (Số lượng đã xuất bàn giao cho shipper)</summary>
        public decimal IssuedQuantity { get; set; } = 0;
    }
}
