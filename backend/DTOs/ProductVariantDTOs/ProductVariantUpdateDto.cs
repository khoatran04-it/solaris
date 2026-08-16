namespace backend.DTOs.ProductVariantDTOs
{
    public class ProductVariantUpdateDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public int InventoryGuideline { get; set; }
        public int ProductId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<AttributeInputDto> Attributes { get; set; } = new();
        public List<VariantPriceInputDto> Prices { get; set; } = new();
    }
}