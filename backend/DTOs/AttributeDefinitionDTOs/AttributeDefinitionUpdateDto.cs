namespace backend.DTOs.AttributeDefinitionDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Thuộc tính trong Từ điển hệ thống.
    /// </summary>
    public class AttributeDefinitionUpdateDto
    {
        #region Thông tin Thuộc tính
        /// <summary>Tên hiển thị của thuộc tính (Ví dụ: "Độ ngọt", "Kích cỡ", "Vùng trồng").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Kiểu dữ liệu của thuộc tính dùng để validate đầu vào (Ví dụ: "String", "Number", "Boolean").</summary>
        public string DataType { get; set; } = string.Empty;
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang cho phép sử dụng, false: Tạm ẩn/khóa).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}