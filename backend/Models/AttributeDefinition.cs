namespace backend.Models
{
    public class AttributeDefinition : ISoftDelete
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string DataType { get; set; }

        // --- AUDIT & SOFT DELETE ---
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}