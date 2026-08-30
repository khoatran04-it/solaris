using System;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Dòng Mặt Hàng Trong Giỏ (Shopping Cart Item).
    /// Đại diện cho một sản phẩm cụ thể (SKU) với số lượng và đơn vị tính mà khách hàng đã chọn 
    /// trước khi tiến hành bước Thanh toán (Checkout).
    /// </summary>
    public class ShoppingCartItem
    {
        public int Id { get; set; }

        #region Liên kết Giỏ hàng (Parent Reference)
        public int CartId { get; set; }
        public virtual ShoppingCart? Cart { get; set; }
        #endregion

        #region Hàng hóa & Đơn vị tính (Product & UoM)
        /// <summary>
        /// Mã định danh Biến thể sản phẩm (SKU) được thêm vào giỏ.
        /// </summary>
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>
        /// Mã định danh Đơn vị tính (Ví dụ: Khách chọn mua 2 "Kg" cà chua hay 3 "Hộp" cà chua).
        /// Cực kỳ quan trọng để hệ thống tính đúng giá tiền và quy đổi tồn kho sau này.
        /// </summary>
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Số lượng & Thời gian
        /// <summary>Số lượng đặt mua (Hỗ trợ kiểu decimal để phục vụ các mặt hàng bán theo trọng lượng như Kg, Gram).</summary>
        public decimal Quantity { get; set; }

        /// <summary>Thời điểm mặt hàng này được thêm lần đầu vào giỏ.</summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Thời điểm số lượng hoặc tùy chọn của mặt hàng này được cập nhật lần cuối.</summary>
        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}