namespace backend.Models
{
    public class ProductAttribute : ISoftDelete
    {
        public int Id { get; set; }
        public int? AttributeDefinitionId { get; set; }
        public virtual AttributeDefinition? AttributeDefinition { get; set; }
        public required string AttributeValue { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }

        // --- AUDIT FIELDS & SOFT DELETE ---
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}