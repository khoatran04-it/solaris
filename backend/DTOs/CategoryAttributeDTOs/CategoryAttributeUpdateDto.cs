namespace backend.DTOs.CategoryAttributeDTOs
{
    public class CategoryAttributeUpdateDto
    {
        public int CategoryId { get; set; }
        public int? AttributeDefinitionId { get; set; }
        public bool IsRequired { get; set; } = false;
    }
}
