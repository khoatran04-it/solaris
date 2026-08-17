using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.AuthDTOs
{
    /// <summary>
    /// Yêu cầu đăng nhập tài khoản nhân viên.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>Tên đăng nhập hệ thống</summary>
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public required string Username { get; set; }

        /// <summary>Mật khẩu đăng nhập</summary>
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        public required string Password { get; set; }
    }

    /// <summary>
    /// Phản hồi kết quả đăng nhập thành công.
    /// </summary>
    public class LoginResponseDto
    {
        /// <summary>Chuỗi JWT Token dùng để xác thực các API tiếp theo</summary>
        public required string Token { get; set; }

        /// <summary>Thông tin chi tiết của nhân viên và danh sách quyền hạn</summary>
        public required IAUserAuthReadDto UserInfo { get; set; }
    }

    /// <summary>
    /// Thông tin tóm tắt của nhân viên sau khi xác thực danh tính.
    /// </summary>
    public class IAUserAuthReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        /// <summary>Danh sách mã các vai trò (Roles) nhân viên nắm giữ</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>Danh sách ID các kho nhân viên được phân quyền thao tác (Data-level Authorization)</summary>
        public List<int> WarehouseIds { get; set; } = new();

        /// <summary>Danh sách tất cả mã quyền thực tế (Sau khi tổng hợp Role và ngoại lệ UserPermission)</summary>
        public List<string> Permissions { get; set; } = new();
    }
}