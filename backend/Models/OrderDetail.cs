namespace backend.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        public decimal Quantity { get; set; }
        // Số lượng quy đổi về Base UoM phục vụ trừ / giữ chỗ tồn kho
        public decimal BaseQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalPrice { get; set; }

        // Tiến độ xuất kho (đã xuất bao nhiêu đơn vị)
        public decimal IssuedQuantity { get; set; } = 0;
    }
}
