namespace backend.Models
{
    /// <summary>
    /// Thực thể Vai trò / Chức vụ trong hệ thống (RBAC).
    /// Ví dụ: Quản trị viên (Admin), Thủ kho (WarehouseKeeper), Kế toán (Accountant), Nhân viên bán hàng (Sales).
    /// </summary>
    public class IARole : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã vai trò viết hoa không dấu (Ví dụ: ADMIN, WAREHOUSE_KEEPER)</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị của vai trò (Ví dụ: Quản Trị Viên, Nhân Viên Kho)</summary>
        public required string Name { get; set; }

        /// <summary>Mô tả chi tiết quyền hạn và trách nhiệm của vai trò</summary>
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