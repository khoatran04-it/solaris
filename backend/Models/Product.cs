namespace backend.Models
{
    /// <summary>
    /// Thực thể Sản Phẩm Cha (Product Master).
    /// Đại diện cho dòng sản phẩm cốt lõi mang tính chất chung (Ví dụ: Táo Envy New Zealand, Cà chua Beef Đà Lạt, Gạo ST25).
    /// Các thông tin bán hàng cụ thể (giá, khối lượng đóng gói, mã SKU) sẽ được quản lý ở bảng biến thể (ProductVariant).
    /// </summary>
    public class Product : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã sản phẩm chung (Ví dụ: PROD-APPLE-ENVY, PROD-TOMATO-BEEF).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của dòng sản phẩm (Ví dụ: Táo Envy New Zealand).</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn thân thiện cho SEO (Ví dụ: tao-envy-new-zealand).</summary>
        public string? Slug { get; set; }
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện chung cho dòng sản phẩm.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả tổng quan, chi tiết về dòng sản phẩm (chất lượng, nguồn gốc chung).</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hiển thị/kinh doanh, false: Tạm ngưng).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh Danh mục trực thuộc (Ví dụ: Trái cây nhập khẩu).</summary>
        public int? CategoryId { get; set; }

        /// <summary>Thực thể Danh mục sản phẩm trực thuộc.</summary>
        public virtual ProductCategory? Category { get; set; }

        /// <summary>Mã định danh Đơn vị tính cơ sở chuẩn của sản phẩm (Base UoM - Ví dụ: Kg, Quả, Cái, Hộp).</summary>
        public int BaseUoMId { get; set; }

        /// <summary>Thực thể Đơn vị tính (Unit of Measure).</summary>
        public virtual UoM? BaseUoM { get; set; }

        /// <summary>Danh sách các Biến thể (SKU bán hàng / Các quy cách đóng gói khác nhau) của dòng sản phẩm này.</summary>
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        #endregion
    }
}