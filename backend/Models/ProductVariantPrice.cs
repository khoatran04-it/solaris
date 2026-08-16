namespace backend.Models
{
    public class ProductVariantPrice : ISoftDelete
    {
        public int Id { get; set; }
        public int VariantId { get; set; }
        public virtual ProductVariant? Variant { get; set; }
        public int UoMId { get; set; }
        public virtual UoM? UoM { get; set; }
        public decimal Price { get; set; }
        public bool IsDefault { get; set; } = false;

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}