namespace backend.Models
{
    public class IAPermission
    {
        public int Id { get; set; }

        // Phân nhóm quyền cho dễ nhìn trên UI (VD: "Quản lý Sản phẩm", "Quản lý Đơn hàng", "Kho hàng")
        public required string Module { get; set; }

        // Mã quyền, dùng để Code check quyền (VD: "PRODUCT_VIEW", "PRODUCT_CREATE", "ORDER_DELETE")
        public required string Code { get; set; }

        // Tên hiển thị (VD: "Xem danh sách sản phẩm")
        public required string Name { get; set; }

        // --- NAVIGATION PROPERTIES ---
        public virtual ICollection<IARolePermission> RolePermissions { get; set; } = new List<IARolePermission>();
        public virtual ICollection<IAUserPermission> UserPermissions { get; set; } = new List<IAUserPermission>();
    }
}