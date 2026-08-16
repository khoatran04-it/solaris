namespace backend.DTOs.ProductDTOs
{
    public class ProductCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; }
        public int? CategoryId { get; set; }
        public int BaseUoMId { get; set; }
    }
}
