namespace backend.DTOs.ProductCategoryDTOs
{
    /// <summary>
    /// DTO yêu cầu cập nhật thông tin Danh Mục Sản Phẩm (Product Category).
    /// </summary>
    public class ProductCategoryUpdateDto
    {
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

        #region Liên kết dữ liệu (Foreign Keys)
        /// <summary>Mã định danh Nhóm ngành hàng lớn (Category Group) mà danh mục này trực thuộc.</summary>
        public int? CategoryGroupId { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hiển thị/sử dụng, false: Tạm ẩn).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Cờ đánh dấu danh mục yêu cầu bảo quản chuỗi lạnh (thịt, cá tươi, rau nhạy cảm).</summary>
        public bool RequiresColdChain { get; set; } = false;
        #endregion
    }
}