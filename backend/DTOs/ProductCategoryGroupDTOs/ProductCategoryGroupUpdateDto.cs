namespace backend.DTOs.ProductCategoryGroupDTOs
{
    public class ProductCategoryGroupUpdateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
