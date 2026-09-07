namespace backend.DTOs.ProductCategoryDTOs
{
    /// <summary>
    /// DTO yêu cầu tạo mới Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public class ProductCategoryCreateDto
    {
        #region Thông tin Định danh
        /// <summary>Mã danh mục (Ví dụ: LEAFY_VEG, FRUITS, ROOT_VEG).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị danh mục (Ví dụ: Rau ăn lá, Trái cây nhập khẩu).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện của danh mục.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về danh mục sản phẩm.</summary>
        public string? Description { get; set; }
        #endregion

        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Nhóm ngành hàng lớn (Category Group) mà danh mục này trực thuộc.</summary>
        public int? CategoryGroupId { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hiển thị/sử dụng, false: Tạm ẩn).</summary>
        public bool IsActive { get; set; } = true;
        #endregion
    }
}