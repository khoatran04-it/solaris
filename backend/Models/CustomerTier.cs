namespace backend.Models
{
    public class CustomerTier : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }

        public decimal DiscountPercent { get; set; } = 0;
        public decimal MinSpending { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }
}