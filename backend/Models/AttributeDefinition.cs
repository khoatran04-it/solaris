namespace backend.Models
{
    /// <summary>
    /// Thực thể Từ điển Thuộc tính (Attribute Definition).
    /// Lưu trữ danh sách chuẩn hóa các thuộc tính động của hệ thống (EAV Pattern - Ví dụ: Độ ngọt, Màu sắc, Kích thước, Xuất xứ) 
    /// cùng với kiểu dữ liệu tương ứng để kiểm soát đầu vào.
    /// </summary>
    public class AttributeDefinition : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Thuộc tính
        /// <summary>Tên hiển thị của thuộc tính (Ví dụ: "Độ ngọt", "Kích cỡ", "Vùng trồng").</summary>
        public required string Name { get; set; }

        /// <summary>Kiểu dữ liệu của thuộc tính dùng để validate (Ví dụ: "String", "Number", "Boolean", "Date").</summary>
        public required string DataType { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép sử dụng, false: Tạm ẩn/khóa).</summary>
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