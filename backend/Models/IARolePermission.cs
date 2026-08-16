namespace backend.Models
{
    public class IARolePermission
    {
        public int RoleId { get; set; }
        public virtual IARole? Role { get; set; }

        public int PermissionId { get; set; }
        public virtual IAPermission? Permission { get; set; }
    }
}