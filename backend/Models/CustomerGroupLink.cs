namespace backend.Models
{
    public class CustomerGroupLink
    {
        // --- FOREIGN KEY ---
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int CustomerGroupId { get; set; }
        public virtual CustomerGroup? CustomerGroup { get; set; }

        public DateTime AssignedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}