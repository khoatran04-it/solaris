namespace backend.Models
{
    public class IARole : ISoftDelete
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<IAUserRole> UserRoles { get; set; } = new List<IAUserRole>();
        public virtual ICollection<IARolePermission> RolePermissions { get; set; } = new List<IARolePermission>();
    }
}