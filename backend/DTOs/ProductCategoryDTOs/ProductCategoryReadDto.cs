namespace backend.DTOs.ProductCategoryDTOs
{
    public class ProductCategoryReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? CategoryGroupId { get; set; }
        public string? CategoryGroupName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
