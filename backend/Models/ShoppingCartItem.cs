namespace backend.Models
{
    /// <summary>
    /// Thực thể Dòng Mặt Hàng Trong Giỏ (Shopping Cart Item).
    /// </summary>
    public class ShoppingCartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }
        public virtual ShoppingCart? Cart { get; set; }

        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }

        /// <summary>Số lượng đặt mua</summary>
        public decimal Quantity { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
