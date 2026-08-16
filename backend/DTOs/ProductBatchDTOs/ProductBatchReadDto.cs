namespace backend.DTOs.ProductBatchDTOs
{
    public class ProductBatchReadDto
    {
        public int Id { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int VariantId { get; set; }
        public int SupplierId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
