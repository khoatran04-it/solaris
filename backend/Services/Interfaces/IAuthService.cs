using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ xác thực tài khoản và phân giải quyền hạn người dùng.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Xác thực thông tin đăng nhập, tổng hợp quyền hạn và khởi tạo JWT Token.
        /// </summary>
        /// <param name="request">Thông tin tài khoản và mật khẩu.</param>
        /// <returns>Chuỗi JWT Token kèm thông tin hồ sơ và danh sách quyền hạn hiệu lực.</returns>
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>
        /// Băm chuỗi mật khẩu thô theo chuẩn BCrypt (hỗ trợ tạo dữ liệu mẫu / kiểm thử).
        /// </summary>
        /// <param name="rawPassword">Chuỗi mật khẩu gốc chưa mã hóa.</param>
        /// <returns>Chuỗi mật khẩu đã được băm.</returns>
        Task<string> HashPasswordAsync(string rawPassword);
    }
}