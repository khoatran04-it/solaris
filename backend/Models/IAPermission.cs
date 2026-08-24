namespace backend.Models
{
    /// <summary>
    /// Thực thể Quyền hạn chi tiết (Granular Permission) trong hệ thống.
    /// Dùng để quản lý và hiển thị ma trận phân quyền theo Module trên giao diện Admin.
    /// </summary>
    public class IAPermission
    {
        public int Id { get; set; }

        /// <summary>Nhóm chức năng/module để gom nhóm trên UI (Ví dụ: "Hệ thống", "Kho hàng", "Bán hàng").</summary>
        public required string Module { get; set; }

        /// <summary>Mã quyền định danh duy nhất theo chuẩn MODULE_ACTION (Ví dụ: "USER_VIEW", "ORDER_APPROVE").</summary>
        public required string Code { get; set; }

        /// <summary>Tên mô tả hiển thị của quyền hạn (Ví dụ: "Xem danh sách người dùng").</summary>
        public required string Name { get; set; }

        #region Navigation Properties
        public virtual ICollection<IARolePermission> RolePermissions { get; set; } = new List<IARolePermission>();
        public virtual ICollection<IAUserPermission> UserPermissions { get; set; } = new List<IAUserPermission>();
        #endregion
    }
}