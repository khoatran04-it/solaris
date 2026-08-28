namespace backend.DTOs.CategoryAttributeDTOs
{
    /// <summary>
    /// DTO hiển thị chi tiết Cấu hình Thuộc tính của Danh mục (EAV Template).
    /// </summary>
    public class CategoryAttributeReadDto
    {
        public int Id { get; set; }

        #region Liên kết dữ liệu & Mở rộng (UI Render)
        /// <summary>Mã định danh Danh mục sản phẩm.</summary>
        public int CategoryId { get; set; }

        /// <summary>Tên hiển thị của Danh mục sản phẩm.</summary>
        public string? CategoryName { get; set; }

        /// <summary>Mã định danh của Thuộc tính từ Từ điển hệ thống.</summary>
        public int? AttributeDefinitionId { get; set; }

        /// <summary>Tên hiển thị của Thuộc tính (Ví dụ: "Độ ngọt", "Xuất xứ", "Kích cỡ").</summary>
        public string? AttributeDefinitionName { get; set; }
        #endregion

        #region Cấu hình Template
        /// <summary>
        /// Cờ đánh dấu thuộc tính này có bắt buộc phải nhập hay không.
        /// </summary>
        public bool IsRequired { get; set; } = false;
        #endregion
    }
}