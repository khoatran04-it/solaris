namespace backend.DTOs.CategoryAttributeDTOs
{
    public class CategoryAttributeReadDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? AttributeDefinitionId { get; set; }
        public string? AttributeDefinitionName { get; set; }
        public bool IsRequired { get; set; } = false;

    }
}
