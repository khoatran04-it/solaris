namespace backend.Models
{
    /// <summary>
    /// Thực thể Giỏ Hàng Khách Hàng (Shopping Cart).
    /// Quản lý giỏ hàng lưu trữ trên Server gắn liền với từng khách hàng (Customer).
    /// </summary>
    public class ShoppingCart
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
    }
}
