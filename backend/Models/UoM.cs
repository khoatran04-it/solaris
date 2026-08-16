namespace backend.Models
{
    public class UoM : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }

        public string? Synonyms { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- FOREIGN KEY ---
        public int? CategoryId { get; set; }
        public virtual UoMCategory? Category { get; set; }
    }
}