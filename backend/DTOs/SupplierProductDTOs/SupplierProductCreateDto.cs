namespace backend.DTOs.SupplierProductDTOs
{
    public class SupplierProductCreateDto
    {
        public string? SupplierSKU { get; set; }
        public decimal LastImportPrice { get; set; }
        public decimal MinimumOrderQuantity { get; set; } = 1m;
        public int LeadTimeDays { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public int VariantId { get; set; }
        public int SupplierId { get; set; }
        public int PurchaseUoMId { get; set; }
    }
}
