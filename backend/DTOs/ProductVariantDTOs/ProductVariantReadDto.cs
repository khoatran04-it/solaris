namespace backend.DTOs.ProductVariantDTOs
{
    public class VariantPriceReadDto
    {
        public int Id { get; set; }
        public int UoMId { get; set; }
        public string? UoMName { get; set; }
        public decimal Price { get; set; }
        public decimal? PromotionalPrice { get; set; }
        public bool IsDefault { get; set; }
    }

    public class ProductVariantReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        public int InventoryGuideline { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public List<VariantAttributeDto> Attributes { get; set; } = new();
        public List<VariantPriceReadDto> Prices { get; set; } = new();
    }

    public class VariantAttributeDto
    {
        public int Id { get; set; }
        public int? AttributeDefinitionId { get; set; }
        public string? AttributeDefinitionName { get; set; }
        public string AttributeValue { get; set; } = string.Empty;
    }
}