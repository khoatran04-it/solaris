namespace backend.DTOs.UoMDTOs
{
    public class UoMCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Synonyms { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
    }
}
