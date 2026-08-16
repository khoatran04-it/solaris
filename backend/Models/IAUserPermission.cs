namespace backend.Models
{
    public class IAUserPermission
    {
        public int UserId { get; set; }
        public virtual IAUser? User { get; set; }

        public int PermissionId { get; set; }
        public virtual IAPermission? Permission { get; set; }

        // Nếu TRUE: Dù Role không có quyền này, User vẫn được CẤP THÊM (Gift).
        // Nếu FALSE: Dù Role có quyền này, User vẫn BỊ TƯỚC BỎ (Ban).
        public bool IsGranted { get; set; }
    }
}