namespace backend.DTOs.ProductCategoryDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public class ProductCategoryReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã danh mục (Ví dụ: LEAFY_VEG, FRUITS).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị danh mục (Ví dụ: Rau ăn lá).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện của danh mục.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về danh mục sản phẩm.</summary>
        public string? Description { get; set; }
        #endregion

        #region Liên kết dữ liệu & Mở rộng (UI Render)
        /// <summary>Mã định danh Nhóm ngành hàng trực thuộc.</summary>
        public int? CategoryGroupId { get; set; }

        /// <summary>Tên hiển thị Nhóm ngành hàng trực thuộc (Lấy từ ProductCategoryGroup).</summary>
        public string? CategoryGroupName { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hiển thị/sử dụng, false: Tạm ẩn).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}