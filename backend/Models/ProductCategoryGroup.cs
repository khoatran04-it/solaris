namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhóm Ngành Hàng Lớn (Product Category Group).
    /// Dùng để phân loại cấp cao nhất cho các sản phẩm (Ví dụ: Nông sản tươi, Thực phẩm khô, Đồ uống đóng chai).
    /// </summary>
    public class ProductCategoryGroup : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã nhóm ngành hàng (Ví dụ: FRESH_PRODUCE, DRY_FOOD).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị nhóm ngành hàng (Ví dụ: Nông sản tươi).</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn thân thiện cho SEO (Ví dụ: nong-san-tuoi, thuc-pham-kho).</summary>
        public string? Slug { get; set; }
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện hoặc Banner của nhóm ngành hàng.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về nhóm ngành hàng.</summary>
        public string? Description { get; set; }
        #endregion

        #region Trạng thái & Hệ thống
        /// <summary>Trạng thái hoạt động (true: Đang hiển thị/sử dụng, false: Tạm ẩn).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        #endregion

        #region Soft Delete
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Liên kết đối tượng (Navigation Properties)
        /// <summary>Danh sách các Danh mục sản phẩm (Category cấp 2) thuộc nhóm ngành hàng này.</summary>
        public virtual ICollection<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
        #endregion
    }
}