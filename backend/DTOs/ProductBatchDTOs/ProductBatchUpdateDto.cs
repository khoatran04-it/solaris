namespace backend.DTOs.ProductBatchDTOs
{
    public class ProductBatchUpdateDto
    {
        public string BatchCode { get; set; } = string.Empty;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int VariantId { get; set; }
        public int SupplierId { get; set; }
    }
}
