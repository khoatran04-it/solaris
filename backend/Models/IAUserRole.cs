namespace backend.Models
{
    public class IAUserRole
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int RoleId { get; set; }
        public virtual IARole? Role { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}