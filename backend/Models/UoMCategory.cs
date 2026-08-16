namespace backend.Models
{
    public class UoMCategory : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // -- FOREIGN KEY ---
        public int? BaseUoMId { get; set; }
        public virtual UoM? BaseUoM { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<UoM> UoMs { get; set; } = new List<UoM>();
    }
}