using backend.DTOs.AuthDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace backend.Tests.Modules.Module01_Auth
{
    /// <summary>
    /// ============================================================================
    /// MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
    /// UNIT TEST: AuthService (Xác thực JWT Token & Phân quyền RBAC)
    /// ============================================================================
    /// </summary>
    public class AuthServiceTests
    {
        private readonly IConfiguration _configuration;

        public AuthServiceTests()
        {
            // Nạp cấu hình JWT Test thực tế từ helper
            _configuration = TestFactories.CreateTestConfiguration();
        }

        #region TEST CASE 01: ĐĂNG NHẬP HỢP LỆ
        /// <summary>
        /// TC01: Khi người dùng cung cấp đúng Username và Mật khẩu,
        /// hệ thống phải trả về chuỗi JWT Token hợp lệ cùng toàn bộ thông tin UserInfo.
        /// </summary>
        [Fact(DisplayName = "TC01 - Đăng nhập thành công khi nhập đúng Username và Mật khẩu")]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnJwtTokenAndUserInfo()
        {
            // Arrange (Chuẩn bị dữ liệu mẫu trong DB ảo)
            using var context = TestFactories.CreateInMemoryDbContext();
            var rawPassword = "SecurePassword@123";
            var user = new IAUser
            {
                Id = 1,
                Username = "admin",
                FullName = "Đặng Khoa",
                Email = "admin@solaris.vn",
                PhoneNumber = "0901234567",
                CitizenId = "079200000001",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
                IsActive = true
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var authService = new AuthService(context, _configuration);
            var request = new LoginRequestDto
            {
                Username = "admin",
                Password = rawPassword
            };

            // Act (Thực hiện hành động)
            var result = await authService.LoginAsync(request);

            // Assert (Khẳng định kết quả)
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrWhiteSpace("Hệ thống phải cấp mã JWT Token cho phiên đăng nhập.");
            result.UserInfo.Username.Should().Be("admin");
            result.UserInfo.FullName.Should().Be("Đặng Khoa");
            result.UserInfo.Email.Should().Be("admin@solaris.vn");
        }
        #endregion

        #region TEST CASE 02: ĐĂNG NHẬP SAI MẬT KHẨU
        /// <summary>
        /// TC02: Khi nhập sai Mật khẩu, hệ thống phải ném ngoại lệ UnauthorizedAccessException
        /// với thông báo lỗi rõ ràng để bảo mật.
        /// </summary>
        [Fact(DisplayName = "TC02 - Đăng nhập thất bại khi nhập sai Mật khẩu")]
        public async Task LoginAsync_WithWrongPassword_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var user = new IAUser
            {
                Id = 2,
                Username = "nhanvien01",
                FullName = "Nhân viên 01",
                Email = "nv01@solaris.vn",
                PhoneNumber = "0901234568",
                CitizenId = "079200000002",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123"),
                IsActive = true
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var authService = new AuthService(context, _configuration);
            var request = new LoginRequestDto
            {
                Username = "nhanvien01",
                Password = "WrongPassword@999" // Mật khẩu không trùng khớp
            };

            // Act & Assert
            var act = async () => await authService.LoginAsync(request);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*không chính xác*");
        }
        #endregion

        #region TEST CASE 03: ĐĂNG NHẬP VỚI USERNAME KHÔNG TỒN TẠI
        /// <summary>
        /// TC03: Khi nhập Username không tồn tại trong hệ thống,
        /// hệ thống phải từ chối truy cập và ném ngoại lệ UnauthorizedAccessException.
        /// </summary>
        [Fact(DisplayName = "TC03 - Đăng nhập thất bại khi Username không tồn tại")]
        public async Task LoginAsync_WithNonExistentUsername_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var authService = new AuthService(context, _configuration);
            var request = new LoginRequestDto
            {
                Username = "non_existent_user",
                Password = "AnyPassword@123"
            };

            // Act & Assert
            var act = async () => await authService.LoginAsync(request);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*không tồn tại*");
        }
        #endregion

        #region TEST CASE 04: ĐĂNG NHẬP VỚI TÀI KHOẢN BỊ KHÓA
        /// <summary>
        /// TC04: Khi tài khoản bị vô hiệu hóa (IsActive = false),
        /// hệ thống phải ngăn chặn đăng nhập và thông báo tài khoản đã bị khóa.
        /// </summary>
        [Fact(DisplayName = "TC04 - Đăng nhập thất bại khi tài khoản đang bị Khóa (IsActive = false)")]
        public async Task LoginAsync_WithLockedAccount_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var user = new IAUser
            {
                Id = 3,
                Username = "locked_user",
                FullName = "Tài khoản khóa",
                Email = "locked@solaris.vn",
                PhoneNumber = "0901234569",
                CitizenId = "079200000003",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                IsActive = false // Trạng thái bị khóa
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var authService = new AuthService(context, _configuration);
            var request = new LoginRequestDto
            {
                Username = "locked_user",
                Password = "Password@123"
            };

            // Act & Assert
            var act = async () => await authService.LoginAsync(request);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*bị khóa*");
        }
        #endregion

        #region TEST CASE 05: TÍNH TOÁN MA TRẬN PHÂN QUYỀN RBAC PHỨC TẠP
        /// <summary>
        /// TC05: Tính toán ma trận quyền hạn thực tế (Effective Permissions):
        /// - Quyền kế thừa từ Vai trò (Role Permissions)
        /// - Quyền cấp thêm trực tiếp cho User (UserPermissions IsGranted = true)
        /// - Quyền tước bỏ trực tiếp của User (UserPermissions IsGranted = false)
        /// </summary>
        [Fact(DisplayName = "TC05 - Tính toán ma trận phân quyền chính xác (Quyền Vai trò + Cấp thêm - Tước bỏ)")]
        public async Task LoginAsync_ShouldCalculateEffectivePermissionsCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();

            var perm1 = new IAPermission { Id = 1, Module = "Hệ thống", Code = "USER_VIEW", Name = "Xem User" };
            var perm2 = new IAPermission { Id = 2, Module = "Hệ thống", Code = "USER_CREATE", Name = "Tạo User" };
            var perm3 = new IAPermission { Id = 3, Module = "Hệ thống", Code = "ROLE_VIEW", Name = "Xem Role" };
            var perm4 = new IAPermission { Id = 4, Module = "Báo cáo", Code = "SPECIAL_REPORT", Name = "Báo cáo đặc biệt" };
            context.IAPermissions.AddRange(perm1, perm2, perm3, perm4);

            var role = new IARole { Id = 1, Code = "STAFF", Name = "Nhân viên", IsActive = true };
            context.IARoles.Add(role);

            // Gán perm1, perm2, perm3 cho vai trò STAFF
            context.IARolePermissions.AddRange(
                new IARolePermission { RoleId = 1, PermissionId = 1, Permission = perm1 },
                new IARolePermission { RoleId = 1, PermissionId = 2, Permission = perm2 },
                new IARolePermission { RoleId = 1, PermissionId = 3, Permission = perm3 }
            );

            var user = new IAUser
            {
                Id = 10,
                Username = "staff_user",
                FullName = "Nhân viên Test",
                Email = "staff@solaris.vn",
                PhoneNumber = "0901234570",
                CitizenId = "079200000010",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123"),
                IsActive = true
            };
            context.IAUsers.Add(user);

            // Gán vai trò STAFF cho user
            context.IAUserRoles.Add(new IAUserRole { UserId = 10, RoleId = 1, Role = role });

            // Tùy biến ngoại lệ cho User:
            // + Cấp thêm: SPECIAL_REPORT (perm4)
            // - Tước bỏ: USER_CREATE (perm2)
            context.IAUserPermissions.AddRange(
                new IAUserPermission { UserId = 10, PermissionId = 4, Permission = perm4, IsGranted = true },
                new IAUserPermission { UserId = 10, PermissionId = 2, Permission = perm2, IsGranted = false }
            );

            await context.SaveChangesAsync();

            var authService = new AuthService(context, _configuration);
            var request = new LoginRequestDto { Username = "staff_user", Password = "Password@123" };

            // Act
            var result = await authService.LoginAsync(request);

            // Assert
            result.UserInfo.Permissions.Should().Contain(new[] { "USER_VIEW", "ROLE_VIEW", "SPECIAL_REPORT" });
            result.UserInfo.Permissions.Should().NotContain("USER_CREATE", "Quyền USER_CREATE đã bị tước bỏ tường minh khỏi tài khoản.");
        }
        #endregion
    }
}
