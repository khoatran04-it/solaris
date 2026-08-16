namespace backend.Models
{
    public class IAUser : ISoftDelete
    {
        public int Id { get; set; }
        public required string CitizenId { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime? LastLoginAt { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<IAUserRole> UserRoles { get; set; } = new List<IAUserRole>();
        public virtual ICollection<IAUserWarehouse> UserWarehouses { get; set; } = new List<IAUserWarehouse>();
        public virtual ICollection<IAUserPermission> UserPermissions { get; set; } = new List<IAUserPermission>();
    }
}