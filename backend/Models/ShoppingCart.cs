using System;
using System.Collections.Generic;

namespace backend.Models
{
    /// <summary>
    /// Thực thể Giỏ Hàng (Shopping Cart).
    /// Quản lý danh sách các mặt hàng mà khách hàng đang chọn mua.
    /// Thiết kế theo mô hình Server-side: Dữ liệu được lưu trong Database để đồng bộ xuyên suốt 
    /// giữa các thiết bị (Web, App) của cùng một khách hàng.
    /// </summary>
    public class ShoppingCart
    {
        public int Id { get; set; }

        #region Khách hàng sở hữu (Ownership)
        /// <summary>
        /// Mã định danh Khách hàng (Customer) sở hữu giỏ hàng này.
        /// Nghiệp vụ: Thiết kế chuẩn thường là quan hệ 1-1 (Mỗi khách hàng chỉ có 1 giỏ hàng Active duy nhất).
        /// </summary>
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }
        #endregion

        #region Thời gian & Cảnh báo Marketing
        /// <summary>Thời điểm giỏ hàng được khởi tạo lần đầu tiên.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Thời điểm giỏ hàng có sự thay đổi lần cuối (Thêm/Sửa/Xóa sản phẩm).
        /// NGHIỆP VỤ MARKETING LÕI: Trường này vô cùng giá trị để chạy các chiến dịch "Abandoned Cart" 
        /// (Gửi Email/Zalo nhắc nhở khách hàng chốt đơn nếu UpdatedAt đã trôi qua 24h mà chưa biến thành Order).
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Liên kết Chi tiết (Navigation)
        /// <summary>
        /// Danh sách các mặt hàng (SKU) và số lượng tương ứng đang nằm trong giỏ.
        /// </summary>
        public virtual ICollection<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();
        #endregion
    }
}