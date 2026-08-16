namespace backend.Models
{
    public class CategoryAttribute
    {
        public int Id { get; set; }

        // Trỏ về Danh mục (ProductCategory)
        public int CategoryId { get; set; }
        public virtual ProductCategory? Category { get; set; }

        // Trỏ về Từ điển thuộc tính (AttributeDefinition)
        public int? AttributeDefinitionId { get; set; }
        public virtual AttributeDefinition? AttributeDefinition { get; set; }

        // Cấu hình: Thuộc tính này có bắt buộc nhân viên phải nhập khi tạo sản phẩm không?
        public bool IsRequired { get; set; } = false;
    }
}