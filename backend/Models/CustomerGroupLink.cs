namespace backend.Models
{
    /// <summary>
    /// Thực thể liên kết Nhiều - Nhiều giữa Khách Hàng và Nhóm Khách Hàng.
    /// </summary>
    public class CustomerGroupLink
    {
        // --- FOREIGN KEY ---
        public int CustomerId { get; set; }
        public virtual Customer? Customer { get; set; }

        public int CustomerGroupId { get; set; }
        public virtual CustomerGroup? CustomerGroup { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}