namespace backend.DTOs.SupplierTypeDTOs
{
    public class SupplierTypeCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

    }
}
