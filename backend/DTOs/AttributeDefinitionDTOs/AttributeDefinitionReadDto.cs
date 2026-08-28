namespace backend.DTOs.AttributeDefinitionDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Thuộc tính từ Từ điển hệ thống (Attribute Definition).
    /// </summary>
    public class AttributeDefinitionReadDto
    {
        public int Id { get; set; }

        #region Thông tin Thuộc tính
        /// <summary>Tên hiển thị của thuộc tính (Ví dụ: "Độ ngọt", "Kích cỡ", "Vùng trồng").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Kiểu dữ liệu của thuộc tính dùng để validate đầu vào (Ví dụ: "String", "Number", "Boolean").</summary>
        public string DataType { get; set; } = string.Empty;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép sử dụng, false: Tạm ẩn/khóa).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}