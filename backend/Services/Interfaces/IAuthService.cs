using backend.DTOs.AuthDTOs;

namespace backend.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        // Hàm phụ trợ để tự tạo Password Hash test thử trong Database
        Task<string> HashPasswordAsync(string rawPassword);
    }
}