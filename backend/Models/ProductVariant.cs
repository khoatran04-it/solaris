namespace backend.Models
{
    /// <summary>
    /// Thực thể Biến Thể Sản Phẩm (SKU - Stock Keeping Unit).
    /// Đây là đơn vị hàng hóa vật lý cụ thể được nhập kho, lưu kho, xuất bán và quản lý tồn kho.
    /// Ví dụ: Cùng là "Táo Envy" (Product Master) nhưng có các biến thể "Size Nhỏ 1kg", "Thùng 10kg".
    /// </summary>
    public class ProductVariant : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã SKU biến thể để quản lý kho (Ví dụ: SKU-ENVY-S-1KG, SKU-ENVY-BOX-10KG).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị chi tiết của biến thể (Ví dụ: Táo Envy Size Nhỏ 1kg).</summary>
        public required string Name { get; set; }
        #endregion

        #region Thông tin Chi tiết & Quy cách
        /// <summary>Đường dẫn ảnh chụp thực tế chi tiết của biến thể này.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về quy cách đóng gói, ngoại hình của riêng biến thể này.</summary>
        public string? Description { get; set; }

        /// <summary>Mức tồn kho an toàn tối thiểu (Safety Stock - hệ thống sẽ tự động cảnh báo khi tồn kho tổng xuống dưới mức này).</summary>
        public int InventoryGuideline { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép kinh doanh/nhập xuất, false: Tạm khóa).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh Sản phẩm cha (Product Master) mà biến thể này trực thuộc.</summary>
        public int ProductId { get; set; }

        /// <summary>Thực thể Sản phẩm cha.</summary>
        public virtual Product? Product { get; set; }

        /// <summary>Danh sách các giá trị thuộc tính động (EAV) của riêng biến thể này (Ví dụ: Độ Brix = 12, Chứng nhận = GlobalGAP).</summary>
        public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();

        /// <summary>Danh sách các Lô hàng (Batch) thực tế đã nhập kho của mã SKU này.</summary>
        public virtual ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();

        /// <summary>Danh sách các Nhà cung cấp (Supplier) có khả năng cung ứng biến thể này.</summary>
        public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

        /// <summary>Danh sách các Chương trình khuyến mãi đang áp dụng cho biến thể này.</summary>
        public virtual ICollection<PromotionVariant> PromotionVariants { get; set; } = new List<PromotionVariant>();

        /// <summary>Bảng giá bán được thiết lập theo các Đơn vị tính khác nhau (Ví dụ: Giá bán lẻ theo Kg, Giá bán sỉ theo Thùng).</summary>
        public virtual ICollection<ProductVariantPrice> Prices { get; set; } = new List<ProductVariantPrice>();
        #endregion
    }
}