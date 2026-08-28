namespace backend.Models
{
    /// <summary>
    /// Thực thể Cấu hình Thuộc tính Danh mục (Category Attribute / EAV Template).
    /// Bảng trung gian kết nối Danh mục (ProductCategory) với Từ điển thuộc tính (AttributeDefinition).
    /// Dùng để định nghĩa khuôn mẫu: Các sản phẩm thuộc danh mục này sẽ có những thuộc tính đặc thù nào.
    /// </summary>
    public class CategoryAttribute
    {
        public int Id { get; set; }

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Mã định danh Danh mục sản phẩm.</summary>
        public int CategoryId { get; set; }

        /// <summary>Thực thể Danh mục sản phẩm.</summary>
        public virtual ProductCategory? Category { get; set; }

        /// <summary>Mã định danh của Thuộc tính từ Từ điển hệ thống (Ví dụ: ID của thuộc tính "Độ ngọt", "Xuất xứ").</summary>
        public int? AttributeDefinitionId { get; set; }

        /// <summary>Thực thể Từ điển thuộc tính.</summary>
        public virtual AttributeDefinition? AttributeDefinition { get; set; }
        #endregion

        #region Cấu hình Template
        /// <summary>
        /// Cờ đánh dấu thuộc tính này có bắt buộc hay không.
        /// Nếu true: Nhân viên bắt buộc phải điền giá trị cho thuộc tính này khi tạo mới sản phẩm thuộc danh mục.
        /// </summary>
        public bool IsRequired { get; set; } = false;
        #endregion
    }
}