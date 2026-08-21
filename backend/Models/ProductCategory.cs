namespace backend.Models
{
    /// <summary>
    /// Thực thể Danh Mục Sản Phẩm (Product Category).
    /// Ví dụ: Rau ăn lá, Củ quả Đà Lạt, Trái cây nhập khẩu, Thịt tươi.
    /// </summary>
    public class ProductCategory : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã danh mục (Ví dụ: LEAFY_VEG, FRUITS, ROOT_VEG)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị danh mục</summary>
        public required string Name { get; set; }

        /// <summary>Đường dẫn thân thiện cho SEO (Ví dụ: trai-cay-nhap-khau)</summary>
        public string? Slug { get; set; }

        /// <summary>Mô tả chi tiết danh mục</summary>
        public string? Description { get; set; }

        /// <summary>Ảnh đại diện danh mục</summary>
        public string? ImagePath { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>Thuộc nhóm ngành hàng nào</summary>
        public int? CategoryGroupId { get; set; }
        public virtual ProductCategoryGroup? CategoryGroup { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        /// <summary>Mẫu các thuộc tính đặc thù được cấu hình sẵn cho danh mục này (EAV Template)</summary>
        public virtual ICollection<CategoryAttribute> AttributeTemplates { get; set; } = new List<CategoryAttribute>();
    }
}