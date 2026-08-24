namespace backend.Models
{
    /// <summary>
    /// Thực thể liên kết Nhiều - Nhiều giữa Vai trò (IARole) và Quyền hạn (IAPermission).
    /// Thiết lập các quyền hạn mặc định theo vai trò trong mô hình RBAC.
    /// Khóa chính phức hợp (Composite Key): (RoleId, PermissionId).
    /// </summary>
    public class IARolePermission
    {
        public int RoleId { get; set; }
        public virtual IARole? Role { get; set; }

        public int PermissionId { get; set; }
        public virtual IAPermission? Permission { get; set; }
    }
}