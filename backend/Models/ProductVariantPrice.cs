namespace backend.Models
{
    /// <summary>
    /// Thực thể Bảng Giá Bán Của Biến Thể Theo Từng Đơn Vị Tính.
    /// Cho phép 1 mã SKU (Biến thể) có nhiều mức giá linh hoạt tương ứng với từng ĐVT (Ví dụ: Cùng SKU Táo Envy nhưng Giá bán lẻ theo Kg, Giá bán sỉ theo Thùng).
    /// </summary>
    public class ProductVariantPrice : ISoftDelete
    {
        public int Id { get; set; }

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU) được thiết lập giá.</summary>
        public int VariantId { get; set; }

        /// <summary>Thực thể Biến thể sản phẩm.</summary>
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Mã định danh Đơn vị tính (UoM - Unit of Measure) áp dụng cho mức giá này.</summary>
        public int UoMId { get; set; }

        /// <summary>Thực thể Đơn vị tính.</summary>
        public virtual UoM? UoM { get; set; }
        #endregion

        #region Thông tin Giá & Cấu hình
        /// <summary>Đơn giá niêm yết bán ra tương ứng với Đơn vị tính (Đơn vị: VNĐ).</summary>
        public decimal Price { get; set; }

        /// <summary>Cờ đánh dấu đây là Đơn vị tính và mức giá bán mặc định khi hiển thị lên Web/App hoặc khi nhân viên tạo đơn hàng mới.</summary>
        public bool IsDefault { get; set; } = false;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái áp dụng giá (true: Mức giá đang có hiệu lực, false: Tạm ngưng áp dụng mức giá này).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion
    }
}