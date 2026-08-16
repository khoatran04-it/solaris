namespace backend.Models
{
    public class SupplierType : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    }
}