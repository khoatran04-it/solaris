namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhân viên / Người dùng trong hệ thống Solaris ERP.
    /// Quản lý thông tin định danh, bảo mật, vai trò (Roles), kho được phân công (Warehouses) và quyền ngoại lệ (Custom Permissions).
    /// </summary>
    public class IAUser : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Căn cước công dân / Mã định danh cá nhân</summary>
        public required string CitizenId { get; set; }

        /// <summary>Tên đăng nhập hệ thống (duy nhất)</summary>
        public required string Username { get; set; }

        /// <summary>Mật khẩu đã được mã hóa BCrypt</summary>
        public required string PasswordHash { get; set; }

        /// <summary>Họ và tên đầy đủ của nhân viên</summary>
        public required string FullName { get; set; }

        /// <summary>Địa chỉ Email liên hệ</summary>
        public required string Email { get; set; }

        /// <summary>Số điện thoại liên hệ</summary>
        public required string PhoneNumber { get; set; }

        /// <summary>Đường dẫn ảnh đại diện</summary>
        public string? AvatarUrl { get; set; }

        /// <summary>Thời điểm đăng nhập gần nhất</summary>
        public DateTime? LastLoginAt { get; set; }

        // --- AUDIT FIELDS ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // --- SOFT DELETE ---
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // --- NAVIGATION PROPERTIES ---
        /// <summary>Danh sách vai trò nhân viên đảm nhiệm</summary>
        public virtual ICollection<IAUserRole> UserRoles { get; set; } = new List<IAUserRole>();

        /// <summary>Danh sách kho nhân viên được phép truy cập và thao tác dữ liệu (Data-level Authorization)</summary>
        public virtual ICollection<IAUserWarehouse> UserWarehouses { get; set; } = new List<IAUserWarehouse>();

        /// <summary>Danh sách quyền tùy biến / ngoại lệ cấp riêng cho nhân viên</summary>
        public virtual ICollection<IAUserPermission> UserPermissions { get; set; } = new List<IAUserPermission>();
    }
}