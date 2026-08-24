namespace backend.Models
{
    /// <summary>
    /// Thực thể quản lý quyền ngoại lệ cấp trực tiếp cho từng Người dùng (Permission Overrides).
    /// Khóa chính phức hợp (Composite Key): (UserId, PermissionId).
    /// </summary>
    public class IAUserPermission
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int PermissionId { get; set; }
        public virtual IAPermission? Permission { get; set; }

        /// <summary>
        /// Trạng thái ghi đè quyền hạn:
        /// <list type="bullet">
        /// <item><description><c>true</c>: Cấp thêm quyền (Allow/Grant) ngay cả khi Vai trò (Role) không có.</description></item>
        /// <item><description><c>false</c>: Chặn/Tước bỏ quyền (Deny/Revoke) dù Vai trò (Role) đang sở hữu.</description></item>
        /// </list>
        /// </summary>
        public bool IsGranted { get; set; }
    }
}