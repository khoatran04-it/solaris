namespace backend.DTOs.AuthDTOs
{
    // Class phụ trợ hứng danh sách Quyền ngoại lệ của từng cá nhân
    public class CustomPermissionDto
    {
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } // True = Tặng thêm, False = Tước bỏ
    }

    // DTO Đọc dữ liệu Nhân viên
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

    public class IAUserCreateDto
    {
        public string CitizenId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Mật khẩu thô (Backend sẽ băm)
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public List<int> RoleIds { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

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

    // DTO Riêng cho trường hợp Reset / Đổi mật khẩu
    public class IAUserChangePasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}