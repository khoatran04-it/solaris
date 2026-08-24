namespace backend.Models
{
    /// <summary>
    /// Thực thể Nhân viên / Người dùng trong hệ thống Solaris ERP.
    /// Quản lý thông tin định danh, bảo mật, vai trò, phân quyền kho và quyền ngoại lệ.
    /// </summary>
    public class IAUser : ISoftDelete
    {
        public int Id { get; set; }

        /// <summary>Mã định danh cá nhân / CCCD.</summary>
        public required string CitizenId { get; set; }

        /// <summary>Tên đăng nhập hệ thống (duy nhất).</summary>
        public required string Username { get; set; }

        /// <summary>Chuỗi mật khẩu đã băm (BCrypt).</summary>
        public required string PasswordHash { get; set; }

        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }

        #region Audit Fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        #endregion

        #region Navigation Properties
        public virtual ICollection<IAUserRole> UserRoles { get; set; } = new List<IAUserRole>();

        /// <summary>Danh sách kho được phép thao tác dữ liệu (Data-level Authorization).</summary>
        public virtual ICollection<IAUserWarehouse> UserWarehouses { get; set; } = new List<IAUserWarehouse>();

        /// <summary>Danh sách quyền tùy biến / ngoại lệ cấp riêng ngoài Role.</summary>
        public virtual ICollection<IAUserPermission> UserPermissions { get; set; } = new List<IAUserPermission>();
        #endregion
    }
}