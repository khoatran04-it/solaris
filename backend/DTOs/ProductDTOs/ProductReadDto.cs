namespace backend.DTOs.ProductDTOs
{
    public class ProductReadDto
    {
        public int Id { get; set; }
        public  string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int BaseUoMId { get; set; }
        public string? BaseUoMName { get; set; }
    }
}
