namespace backend.DTOs.SupplierProductDTOs
{
    public class SupplierProductReadDto
    {
        public int Id { get; set; }
        public string? SupplierSKU { get; set; }
        public decimal LastImportPrice { get; set; }
        public decimal MinimumOrderQuantity { get; set; } = 1m;
        public int LeadTimeDays { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Foreign Key IDs
        public int VariantId { get; set; }
        public int SupplierId { get; set; }
        public int PurchaseUoMId { get; set; }

        // Navigation Enriched Properties for Fast UI Render
        public string? VariantCode { get; set; }
        public string? VariantName { get; set; }
        public string? VariantImagePath { get; set; }

        public string? SupplierCode { get; set; }
        public string? SupplierName { get; set; }

        public string? PurchaseUoMName { get; set; }
    }
}
