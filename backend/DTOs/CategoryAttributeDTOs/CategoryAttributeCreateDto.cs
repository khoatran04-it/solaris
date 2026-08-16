namespace backend.DTOs.CategoryAttributeDTOs
{
    public class CategoryAttributeCreateDto
    {
        public int CategoryId { get; set; }
        public int? AttributeDefinitionId { get; set; }
        public bool IsRequired { get; set; } = false;
    }
}
