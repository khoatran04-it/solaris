using backend.Data;
using backend.DTOs.AuthDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Services
{
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
            // 1. Tìm User và "kéo" theo toàn bộ dây nhợ (Roles, Kho, Quyền)
            var user = await _context.IAUsers
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Include(u => u.UserWarehouses)
                .Include(u => u.UserPermissions)
                    .ThenInclude(up => up.Permission)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // Kiểm tra trạng thái tài khoản
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị khóa.");

            // 2. Kiểm tra Mật khẩu bằng BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Mật khẩu không chính xác.");

            // 3. Cập nhật thời gian đăng nhập
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 4. Lấy danh sách Roles (Chỉ lấy Role đang Active)
            var activeRoles = user.UserRoles
                .Where(ur => ur.Role != null && ur.Role.IsActive)
                .Select(ur => ur.Role!)
                .ToList();

            var roleCodes = activeRoles.Select(r => r.Code).ToList();
            var warehouseIds = user.UserWarehouses.Select(uw => uw.WarehouseId).ToList();

            // ==============================================================
            // 🔥 BƯỚC 5: TÍNH TOÁN MA TRẬN PHÂN QUYỀN (CÔNG THỨC TOÁN HỌC)
            // Quyền Thực Tế = [Quyền Mặc Định] + [Tặng Thêm] - [Tước Bỏ]
            // ==============================================================

            // A. Lấy Quyền Mặc Định từ các Role
            var defaultPermissions = activeRoles
                .SelectMany(r => r.RolePermissions)
                .Where(rp => rp.Permission != null)
                .Select(rp => rp.Permission!.Code)
                .ToList();

            // B. Lấy Quyền Tặng Thêm (IsGranted = true)
            var grantedPermissions = user.UserPermissions
                .Where(up => up.IsGranted && up.Permission != null)
                .Select(up => up.Permission!.Code)
                .ToList();

            // C. Lấy Quyền Bị Tước (IsGranted = false)
            var revokedPermissions = user.UserPermissions
                .Where(up => !up.IsGranted && up.Permission != null)
                .Select(up => up.Permission!.Code)
                .ToList();

            // D. Gộp lại và chốt hạ (Union = Gom không trùng, Except = Loại trừ)
            var finalPermissions = defaultPermissions
                .Union(grantedPermissions)
                .Except(revokedPermissions)
                .Distinct()
                .ToList();

            // 6. Tạo Token JWT
            var token = GenerateJwtToken(user, roleCodes, warehouseIds, finalPermissions);

            // 7. Trả về kết quả
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
                    Permissions = finalPermissions // Gửi xuống cho Frontend giấu nút bấm
                }
            };
        }

        public Task<string> HashPasswordAsync(string rawPassword)
        {
            return Task.FromResult(BCrypt.Net.BCrypt.HashPassword(rawPassword));
        }

        // --- HÀM NỘI BỘ TẠO TOKEN ---
        private string GenerateJwtToken(IAUser user, List<string> roles, List<int> warehouseIds, List<string> permissions)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            // Đóng gói Claims cơ bản
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName)
            };

            // Nhét Roles vào Token
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Nhét Các Quyền vào Token (Để API Backend check Authorize)
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("Permission", perm));
            }

            // Nhét WarehouseIds vào Token
            if (warehouseIds.Any())
            {
                claims.Add(new Claim("WarehouseIds", string.Join(",", warehouseIds)));
            }

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"]));

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}