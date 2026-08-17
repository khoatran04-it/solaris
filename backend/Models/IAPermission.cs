namespace backend.Models
{
    /// <summary>
    /// Thực thể Quyền hạn chi tiết trong hệ thống (Granular Permission).
    /// Được nhóm theo Module để phục vụ ma trận phân quyền trực quan trên giao diện Admin.
    /// </summary>
    public class IAPermission
    {
        public int Id { get; set; }

        /// <summary>Phân nhóm module (Ví dụ: "Hệ thống", "Sản phẩm", "Kho hàng", "Bán hàng")</summary>
        public required string Module { get; set; }

        /// <summary>Mã định danh quyền hạn (Ví dụ: "USER_VIEW", "PRODUCT_CREATE", "ORDER_APPROVE")</summary>
        public required string Code { get; set; }

        /// <summary>Tên hiển thị tiếng Việt của quyền hạn</summary>
        public required string Name { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<IARolePermission> RolePermissions { get; set; } = new List<IARolePermission>();
        public virtual ICollection<IAUserPermission> UserPermissions { get; set; } = new List<IAUserPermission>();
    }
}