namespace backend.DTOs.ProductCategoryDTOs
{
    public class ProductCategoryCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public int? CategoryGroupId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
