namespace backend.DTOs.ProductCategoryGroupDTOs
{
    /// <summary>
    /// DTO hiển thị thông tin chi tiết Nhóm Ngành Hàng lớn (Product Category Group).
    /// </summary>
    public class ProductCategoryGroupReadDto
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã nhóm ngành hàng (Ví dụ: FRESH_PRODUCE, DRY_FOOD).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên hiển thị nhóm ngành hàng (Ví dụ: Nông sản tươi, Thực phẩm khô).</summary>
        public string Name { get; set; } = string.Empty;
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện hoặc Banner của nhóm ngành hàng.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về nhóm ngành hàng.</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hiển thị/sử dụng, false: Tạm ẩn).</summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        #endregion
    }
}