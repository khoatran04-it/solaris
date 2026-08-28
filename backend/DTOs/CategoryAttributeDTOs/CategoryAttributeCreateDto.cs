namespace backend.DTOs.CategoryAttributeDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Cấu hình Thuộc tính cho Danh mục (EAV Template).
    /// </summary>
    public class CategoryAttributeCreateDto
    {
        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Danh mục sản phẩm.</summary>
        public int CategoryId { get; set; }

        /// <summary>Mã định danh của Thuộc tính từ Từ điển hệ thống (AttributeDefinition).</summary>
        public int? AttributeDefinitionId { get; set; }
        #endregion

        #region Cấu hình Template
        /// <summary>
        /// Cờ đánh dấu thuộc tính này có bắt buộc hay không.
        /// (Nếu true: Nhân viên bắt buộc phải nhập giá trị này khi tạo mới sản phẩm thuộc danh mục trên).
        /// </summary>
        public bool IsRequired { get; set; } = false;
        #endregion
    }
}