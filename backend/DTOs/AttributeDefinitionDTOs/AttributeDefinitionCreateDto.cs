namespace backend.DTOs.AttributeDefinitionDTOs
{
    public class AttributeDefinitionCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
