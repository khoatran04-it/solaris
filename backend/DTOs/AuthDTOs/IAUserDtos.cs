namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// DTO thiết lập quyền ngoại lệ trực tiếp cho người dùng.
    /// </summary>
    public class CustomPermissionDto
    {
        public int PermissionId { get; set; }

        /// <summary>true = Cấp thêm quyền (Allow); false = Tước bỏ quyền từ Vai trò (Deny).</summary>
        public bool IsGranted { get; set; }
    }

    /// <summary>
    /// DTO hiển thị thông tin chi tiết người dùng và cấu hình phân quyền.
    /// </summary>
    public class IAUserReadDto
    {
        public int Id { get; set; }
        public string CitizenId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<int> RoleIds { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

    /// <summary>
    /// DTO yêu cầu tạo mới tài khoản người dùng kèm gán quyền ban đầu.
    /// </summary>
    public class IAUserCreateDto
    {
        public string CitizenId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public List<int> RoleIds { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

    /// <summary>
    /// DTO yêu cầu cập nhật thông tin hồ sơ và cấu trúc phân quyền của người dùng.
    /// </summary>
    public class IAUserUpdateDto
    {
        public string CitizenId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }

        public List<int> RoleIds { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

    /// <summary>
    /// DTO yêu cầu thay đổi mật khẩu tài khoản.
    /// </summary>
    public class IAUserChangePasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}