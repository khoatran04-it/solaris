using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// DTO đại diện cho một quyền ngoại lệ được cấp thêm hoặc tước bỏ riêng cho một nhân viên cụ thể.
    /// </summary>
    public class CustomPermissionDto
    {
        public int PermissionId { get; set; }

        /// <summary>True = Cấp thêm quyền; False = Tước bỏ quyền từ Role</summary>
        public bool IsGranted { get; set; }
    }

    /// <summary>
    /// DTO hiển thị thông tin chi tiết nhân viên trong hệ thống.
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

        /// <summary>Danh sách ID các Vai trò nhân viên thuộc về</summary>
        public List<int> RoleIds { get; set; } = new();

        /// <summary>Danh sách ID các Kho hàng nhân viên được phân công phụ trách</summary>
        public List<int> WarehouseIds { get; set; } = new();

        /// <summary>Danh sách quyền tùy biến / ngoại lệ cấp riêng</summary>
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

    /// <summary>
    /// DTO tạo mới tài khoản nhân viên.
    /// </summary>
    public class IAUserCreateDto
    {
        [Required(ErrorMessage = "Số CCCD không được để trống.")]
        [StringLength(20, ErrorMessage = "Số CCCD tối đa 20 ký tự.")]
        public string CitizenId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [StringLength(150, ErrorMessage = "Họ và tên tối đa 150 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public List<int> RoleIds { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

    /// <summary>
    /// DTO cập nhật hồ sơ nhân viên.
    /// </summary>
    public class IAUserUpdateDto
    {
        [Required(ErrorMessage = "Số CCCD không được để trống.")]
        public string CitizenId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }

        public List<int> RoleIds { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();
        public List<CustomPermissionDto> CustomPermissions { get; set; } = new();
    }

    /// <summary>
    /// DTO đổi mật khẩu nhân viên.
    /// </summary>
    public class IAUserChangePasswordDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}