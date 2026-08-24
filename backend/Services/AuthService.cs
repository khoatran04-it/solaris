using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.DTOs.AuthDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services
{
    /// <summary>
    /// Dịch vụ xử lý xác thực danh tính, tính toán ma trận phân quyền và cấp phát JWT Token.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly SolarisDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(SolarisDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            #region 1. Truy vấn người dùng và nạp quan hệ liên quan (Eager Loading)
            var user = await _context.IAUsers
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWarehouses)
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị khóa.");
            }
            #endregion

            #region 2. Xác thực mật khẩu
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // Ghi nhận thời điểm đăng nhập thành công gần nhất
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            #endregion

            #region 3. Trích xuất vai trò & kho được phân quyền
            var activeRoles = user.UserRoles
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .Select(ur => ur.Role!)
                .ToList();

            var roleCodes = activeRoles.Select(r => r.Code).ToList();
            var warehouseIds = user.UserWarehouses.Select(uw => uw.WarehouseId).ToList();
            #endregion

            #region 4. Tính toán ma trận phân quyền (Quyền mặc định + Cấp thêm - Tước bỏ)
            // A. Tập quyền mặc định kế thừa từ tất cả vai trò đang kích hoạt
            var defaultPermissions = activeRoles
                .SelectMany(r => r.RolePermissions)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.Code);

            // B. Tập quyền được cấp thêm riêng cho tài khoản (Override: Allow)
            var grantedPermissions = user.UserPermissions
                .Where(up => up.IsGranted && up.Permission != null)
                .Select(up => up.Permission!.Code);

            // C. Tập quyền bị tước bỏ riêng đối với tài khoản (Override: Deny)
            var revokedPermissions = user.UserPermissions
                .Where(up => !up.IsGranted && up.Permission != null)
                .Select(up => up.Permission!.Code);

            // D. Tổng hợp tập quyền hiệu lực thực tế (Effective Permissions)
            var finalPermissions = defaultPermissions
                .Union(grantedPermissions)
                .Except(revokedPermissions)
                .Distinct()
                .ToList();
            #endregion

            #region 5. Sinh JWT Token & Đóng gói DTO phản hồi
            var token = GenerateJwtToken(user, roleCodes, warehouseIds, finalPermissions);

            return new LoginResponseDto
            {
                Token = token,
                UserInfo = new IAUserAuthReadDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    Roles = roleCodes,
                    WarehouseIds = warehouseIds,
                    Permissions = finalPermissions
                }
            };
            #endregion
        }

        public Task<string> HashPasswordAsync(string rawPassword)
        {
            return Task.FromResult(BCrypt.Net.BCrypt.HashPassword(rawPassword));
        }

        #region Helper: Cấp phát JWT Token
        private string GenerateJwtToken(IAUser user, List<string> roles, List<int> warehouseIds, List<string> permissions)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("Chưa cấu hình SecretKey trong JwtSettings.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // Khởi tạo các Claims tiêu chuẩn định danh người dùng
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("FullName", user.FullName)
            };

            // Đính kèm danh sách Vai trò (Roles)
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Đính kèm các Quyền chi tiết (Granular Permissions) phục vụ Policy Authorization
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("Permission", perm));
            }

            // Đính kèm danh sách ID kho để kiểm tra phân quyền dữ liệu (Data-level Authorization)
            if (warehouseIds.Any())
            {
                claims.Add(new Claim("WarehouseIds", string.Join(",", warehouseIds)));
            }

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = Convert.ToDouble(jwtSettings["ExpiryMinutes"] ?? "1440");
            var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        #endregion
    }
}