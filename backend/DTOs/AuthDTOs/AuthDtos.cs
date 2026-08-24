using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// DTO yêu cầu đăng nhập hệ thống.
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public required string Password { get; set; }
    }

    /// <summary>
    /// DTO kết quả phản hồi khi xác thực đăng nhập thành công.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>Chuỗi JWT Token để đính kèm vào Header Authorization của các yêu cầu API tiếp theo.</summary>
        public required string Token { get; set; }

        /// <summary>Thông tin người dùng và tập quyền hạn phục vụ điều hướng trên giao diện (Client-side Routing/RBAC).</summary>
        public required IAUserAuthReadDto UserInfo { get; set; }
    }

    /// <summary>
    /// DTO thông tin tóm tắt và tập quyền đã phân giải của người dùng sau khi xác thực.
    /// </summary>
    public class IAUserAuthReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        /// <summary>Danh sách mã vai trò (Ví dụ: ["ADMIN", "WAREHOUSE_KEEPER"]).</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>Danh sách ID kho được phép truy cập và thao tác dữ liệu (Data Scoping).</summary>
        public List<int> WarehouseIds { get; set; } = new();

        /// <summary>Tập hợp mã quyền hạn hiệu lực thực tế sau khi tính toán (Role Permissions + Custom Overrides).</summary>
        public List<string> Permissions { get; set; } = new();
    }
}