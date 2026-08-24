namespace backend.Models
{
    /// <summary>
    /// Thực thể liên kết Nhiều - Nhiều giữa Người dùng (IAUser) và Vai trò (IARole).
    /// Khóa chính phức hợp (Composite Key): (UserId, RoleId).
    /// </summary>
    public class IAUserRole
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int RoleId { get; set; }
        public virtual IARole? Role { get; set; }

        /// <summary>Thời điểm gán vai trò cho người dùng (UTC).</summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}