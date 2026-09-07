namespace backend.Models
{
    /// <summary>
    /// Thực thể Danh Mục Sản Phẩm (Product Category).
    /// Phân loại cấp trung gian cho các sản phẩm (Ví dụ: Rau ăn lá, Củ quả, Trái cây nhập khẩu, Thịt tươi).
    /// </summary>
    public class ProductCategory : ISoftDelete
    {
        public int Id { get; set; }

        #region Thông tin Định danh
        /// <summary>Mã danh mục (Ví dụ: LEAFY_VEG, FRUITS, ROOT_VEG).</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị danh mục (Ví dụ: Rau ăn lá).</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn thân thiện cho SEO (Ví dụ: trai-cay-nhap-khau).</summary>
        public string? Slug { get; set; }
        #endregion

        #region Thông tin Chi tiết
        /// <summary>Đường dẫn ảnh đại diện của danh mục.</summary>
        public string? ImagePath { get; set; }

        /// <summary>Mô tả chi tiết về danh mục sản phẩm.</summary>
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
        /// <summary>Mã định danh Nhóm ngành hàng lớn (Category Group) mà danh mục này trực thuộc.</summary>
        public int? CategoryGroupId { get; set; }

        /// <summary>Thực thể Nhóm ngành hàng trực thuộc.</summary>
        public virtual ProductCategoryGroup? CategoryGroup { get; set; }

        /// <summary>Danh sách các Sản phẩm (Product gốc) thuộc danh mục này.</summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        /// <summary>Danh sách mẫu thuộc tính đặc thù được cấu hình sẵn cho danh mục này (EAV Template - ví dụ: Độ ngọt, Xuất xứ).</summary>
        public virtual ICollection<CategoryAttribute> AttributeTemplates { get; set; } = new List<CategoryAttribute>();
        #endregion
    }
}