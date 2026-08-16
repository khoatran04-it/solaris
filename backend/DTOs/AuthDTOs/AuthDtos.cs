namespace backend.DTOs.AuthDTOs
{
    public class LoginRequestDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class LoginResponseDto
    {
        public required string Token { get; set; } // Thẻ JWT dùng để gọi API
        public required IAUserAuthReadDto UserInfo { get; set; } // Thông tin hiển thị UI
    }

    public class IAUserAuthReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public List<string> Roles { get; set; } = new();
        public List<int> WarehouseIds { get; set; } = new();

        // 🔥 DANH SÁCH QUYỀN THỰC TẾ (Sau khi đã cộng trừ)
        public List<string> Permissions { get; set; } = new();
    }
}