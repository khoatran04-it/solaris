namespace backend.Models
{
    /// <summary>
    /// Vai trò người dùng trong hệ thống (RBAC).
    /// </summary>
    public class IARole : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>
        /// Mã định danh vai trò, viết hoa không dấu (VD: ADMIN, WAREHOUSE_KEEPER).
        /// </summary>
        public required string Code { get; set; }

        public required string Name { get; set; }
        public string? Description { get; set; }

        #region Audit & Soft Delete
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Navigation Properties
        public virtual ICollection<IAUserRole> UserRoles { get; set; } = new List<IAUserRole>();
        public virtual ICollection<IARolePermission> RolePermissions { get; set; } = new List<IARolePermission>();
        #endregion
    }
}