namespace backend.Models
{
    /// <summary>
    /// Thực thể Giá trị Thuộc tính Sản phẩm (Product Attribute / EAV Value).
    /// Lưu trữ giá trị cụ thể của một thuộc tính cho một biến thể sản phẩm (Ví dụ: Biến thể "Cà chua hộp 500g" có giá trị thuộc tính "Độ ngọt" là "Brix 5").
    /// </summary>
    public class ProductAttribute : ISoftDelete
    {
        public int Id { get; set; }

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh của Biến thể sản phẩm (SKU hệ thống) sở hữu giá trị này.</summary>
        public int VariantId { get; set; }

        /// <summary>Thực thể Biến thể sản phẩm.</summary>
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Mã định danh Từ điển thuộc tính (Định nghĩa thuộc tính này là gì, ví dụ: Màu sắc, Khối lượng).</summary>
        public int? AttributeDefinitionId { get; set; }

        /// <summary>Thực thể Từ điển thuộc tính hệ thống.</summary>
        public virtual AttributeDefinition? AttributeDefinition { get; set; }
        #endregion

        #region Thông tin Giá trị & Hiển thị
        /// <summary>Giá trị thực tế của thuộc tính được gán cho biến thể này.
        /// (Được lưu dưới dạng chuỗi linh hoạt, nhưng khi nhập liệu sẽ được validate dựa trên DataType của AttributeDefinition).</summary>
        public required string AttributeValue { get; set; }

        /// <summary>Vị trí ưu tiên hiển thị của thuộc tính trên giao diện người dùng (số càng nhỏ càng xếp trên).</summary>
        public int DisplayOrder { get; set; } = 0;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hiển thị (true: Đang hiển thị cho người dùng, false: Tạm ẩn).</summary>
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