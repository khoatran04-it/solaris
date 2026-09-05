using backend.DTOs.AuthDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;

namespace backend.Tests.Modules.Module01_Auth
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
    /// 🧪 UNIT TEST: IAUserService (Quản lý Hồ sơ Nhân viên & Cấp quyền Tài khoản)
    /// ============================================================================
    /// </summary>
    public class IAUserServiceTests
    {
        #region TEST CASE 01: PHÂN TRANG VÀ TÌM KIẾM NHÂN VIÊN
        /// <summary>
        /// TC01: Phân trang danh sách nhân viên và tìm kiếm theo từ khóa (Username, Họ tên, Email, SĐT).
        /// </summary>
        [Fact(DisplayName = "TC01 - Phân trang và tìm kiếm nhân viên theo từ khóa")]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            context.IAUsers.AddRange(
                new IAUser { Id = 1, Username = "khoatran", FullName = "Đặng Trần Khoa", Email = "khoa@solaris.vn", PhoneNumber = "0901111111", CitizenId = "079200000001", PasswordHash = "hash" },
                new IAUser { Id = 2, Username = "linhnguyen", FullName = "Nguyễn Mỹ Linh", Email = "linh@solaris.vn", PhoneNumber = "0902222222", CitizenId = "079200000002", PasswordHash = "hash" },
                new IAUser { Id = 3, Username = "huyhoang", FullName = "Hoàng Gia Huy", Email = "huy@solaris.vn", PhoneNumber = "0903333333", CitizenId = "079200000003", PasswordHash = "hash" }
            );
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);

            // Act: Tìm từ khóa "Mỹ Linh"
            var result = await userService.GetPagedAsync("Mỹ Linh", null, null, null, 1, 10);

            // Assert
            result.TotalRecords.Should().Be(1);
            result.Items.Should().ContainSingle(u => u.Username == "linhnguyen");
        }
        #endregion

        #region TEST CASE 02: LẤY CHI TIẾT NHÂN VIÊN KÈM ROLES & KHO
        /// <summary>
        /// TC02: Lấy thông tin chi tiết nhân viên theo ID kèm danh sách RoleIds, WarehouseIds và CustomPermissions.
        /// </summary>
        [Fact(DisplayName = "TC02 - Lấy chi tiết nhân viên theo ID kèm RoleIds, WarehouseIds và CustomPermissions")]
        public async Task GetByIdAsync_ShouldReturnUserDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var user = new IAUser
            {
                Id = 15,
                Username = "lead_sales",
                FullName = "Trưởng Phòng Sales",
                Email = "sales@solaris.vn",
                PhoneNumber = "0909999888",
                CitizenId = "079200000015",
                PasswordHash = "hash"
            };
            context.IAUsers.Add(user);

            context.IAUserRoles.Add(new IAUserRole { UserId = 15, RoleId = 2 });
            context.IAUserWarehouses.Add(new IAUserWarehouse { UserId = 15, WarehouseId = 1 });
            context.IAUserPermissions.Add(new IAUserPermission { UserId = 15, PermissionId = 99, IsGranted = true });
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);

            // Act
            var result = await userService.GetByIdAsync(15);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("lead_sales");
            result.RoleIds.Should().Contain(2);
            result.WarehouseIds.Should().Contain(1);
            result.CustomPermissions.Should().ContainSingle(cp => cp.PermissionId == 99 && cp.IsGranted);
        }
        #endregion

        #region TEST CASE 03: CHẶN TRÙNG USERNAME
        /// <summary>
        /// TC03: Không cho phép tạo tài khoản có Username đã tồn tại trong hệ thống.
        /// </summary>
        [Fact(DisplayName = "TC03 - Tạo nhân viên thất bại khi trùng Tên đăng nhập (Username)")]
        public async Task CreateAsync_WithDuplicateUsername_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            context.IAUsers.Add(new IAUser
            {
                Username = "khoatran",
                FullName = "Trần Khoa",
                Email = "khoa1@solaris.vn",
                PhoneNumber = "0901111111",
                CitizenId = "079200000001",
                PasswordHash = "hash"
            });
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);
            var createDto = new IAUserCreateDto
            {
                Username = "khoatran", // Trùng Username
                FullName = "Khoa Trần 2",
                Email = "khoa2@solaris.vn",
                PhoneNumber = "0902222222",
                CitizenId = "079200000002",
                Password = "Password@123"
            };

            // Act & Assert
            var act = async () => await userService.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại trong hệ thống*");
        }
        #endregion

        #region TEST CASE 04: CHẶN TRÙNG EMAIL / SĐT / CCCD
        /// <summary>
        /// TC04: Không cho phép tạo tài khoản có Email, Số điện thoại hoặc CCCD bị trùng lặp.
        /// </summary>
        [Fact(DisplayName = "TC04 - Tạo nhân viên thất bại khi trùng Email hoặc Số điện thoại")]
        public async Task CreateAsync_WithDuplicateEmailOrPhone_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            context.IAUsers.Add(new IAUser
            {
                Username = "existing_user",
                FullName = "Nhân viên cũ",
                Email = "duplicate@solaris.vn",
                PhoneNumber = "0987654321",
                CitizenId = "079100000000",
                PasswordHash = "hash"
            });
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);
            var createDto = new IAUserCreateDto
            {
                Username = "new_unique_user",
                FullName = "Nhân viên mới",
                Email = "duplicate@solaris.vn", // Trùng email
                PhoneNumber = "0911223344",
                CitizenId = "079200000099",
                Password = "Password@123"
            };

            // Act & Assert
            var act = async () => await userService.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Email*đã được sử dụng bởi tài khoản khác*");
        }
        #endregion

        #region TEST CASE 05: TẠO NHÂN VIÊN THÀNH CÔNG (BĂM MẬT KHẨU & GÁN VAI TRÒ/KHO)
        /// <summary>
        /// TC05: Tạo nhân viên mới thành công: Băm mật khẩu BCrypt, gán RoleIds và WarehouseIds vào DB.
        /// </summary>
        [Fact(DisplayName = "TC05 - Tạo nhân viên thành công: Tự động băm mật khẩu và gán Vai trò/Kho ban đầu")]
        public async Task CreateAsync_WithValidData_ShouldHashPasswordAndAssignRolesAndWarehouses()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var role = new IARole { Id = 5, Code = "MANAGER", Name = "Quản lý kho" };
            var warehouse = new Warehouse { Id = 3, Code = "WH-TD", Name = "Kho Thủ Đức" };
            context.IARoles.Add(role);
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);
            var createDto = new IAUserCreateDto
            {
                Username = "new_manager",
                FullName = "Nguyễn Văn Quản Lý",
                Email = "manager@solaris.vn",
                PhoneNumber = "0988776655",
                CitizenId = "079300001122",
                Password = "RawPassword@123",
                RoleIds = new List<int> { 5 },
                WarehouseIds = new List<int> { 3 }
            };

            // Act
            var newUserId = await userService.CreateAsync(createDto);

            // Assert
            newUserId.Should().BeGreaterThan(0);

            var createdUser = await userService.GetByIdAsync(newUserId);
            createdUser.Should().NotBeNull();
            createdUser!.Username.Should().Be("new_manager");
            createdUser.RoleIds.Should().Contain(5);
            createdUser.WarehouseIds.Should().Contain(3);

            // Kiểm tra mật khẩu trong DB đã được băm bằng BCrypt (không lưu plain-text)
            var rawUserInDb = await context.IAUsers.FindAsync(newUserId);
            rawUserInDb!.PasswordHash.Should().NotBe("RawPassword@123");
            BCrypt.Net.BCrypt.Verify("RawPassword@123", rawUserInDb.PasswordHash).Should().BeTrue();
        }
        #endregion

        #region TEST CASE 06: BẬT / TẮT TRẠNG THÁI TÀI KHOẢN (TOGGLE ACTIVE)
        /// <summary>
        /// TC06: Bật/Tắt trạng thái hoạt động của tài khoản nhân viên.
        /// </summary>
        [Fact(DisplayName = "TC06 - Đổi trạng thái Hoạt động/Tạm khóa của nhân viên (ToggleActive)")]
        public async Task ToggleActiveAsync_ShouldFlipIsActiveStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var user = new IAUser
            {
                Id = 100,
                Username = "user_to_lock",
                FullName = "User Khóa",
                Email = "lock@solaris.vn",
                PhoneNumber = "0901999999",
                CitizenId = "079200000100",
                PasswordHash = "hash",
                IsActive = true
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);

            // Act lần 1: Khóa tài khoản
            var isNowActive1 = await userService.ToggleActiveAsync(100);
            isNowActive1.Should().BeFalse();

            // Act lần 2: Mở khóa tài khoản
            var isNowActive2 = await userService.ToggleActiveAsync(100);
            isNowActive2.Should().BeTrue();
        }
        #endregion

        #region TEST CASE 07: ĐỔI MẬT KHẨU TÀI KHOẢN
        /// <summary>
        /// TC07: Đổi mật khẩu tài khoản người dùng, mật khẩu mới được băm BCrypt.
        /// </summary>
        [Fact(DisplayName = "TC07 - Đổi mật khẩu nhân viên thành công và băm mật khẩu mới")]
        public async Task ChangePasswordAsync_ShouldUpdatePasswordHash()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var user = new IAUser
            {
                Id = 200,
                Username = "user_change_pass",
                FullName = "User Đổi Pass",
                Email = "changepass@solaris.vn",
                PhoneNumber = "0901888777",
                CitizenId = "079200000200",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPass@123"),
                IsActive = true
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);

            // Act: Đổi mật khẩu mới
            var isSuccess = await userService.ChangePasswordAsync(200, new IAUserChangePasswordDto { NewPassword = "NewPass@456" });

            // Assert
            isSuccess.Should().BeTrue();
            var userInDb = await context.IAUsers.FindAsync(200);
            BCrypt.Net.BCrypt.Verify("NewPass@456", userInDb!.PasswordHash).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify("OldPass@123", userInDb.PasswordHash).Should().BeFalse();
        }
        #endregion

        #region TEST CASE 08 & 09: BẢO VỆ TÀI KHOẢN ROOT ADMIN
        [Fact(DisplayName = "TC08 - Chặn xóa tài khoản admin gốc tối cao")]
        public async Task DeleteAsync_AdminRootUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var user = new IAUser
            {
                Id = 1,
                Username = "admin",
                FullName = "Quản trị viên tối cao",
                Email = "admin@solaris.vn",
                PhoneNumber = "0900000000",
                CitizenId = "000000000001",
                PasswordHash = "hash",
                IsActive = true
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);

            // Act & Assert
            var act = async () => await userService.DeleteAsync(1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa tài khoản quản trị viên tối cao*");
        }

        [Fact(DisplayName = "TC09 - Chặn tạm khóa tài khoản admin gốc tối cao")]
        public async Task ToggleActiveAsync_AdminRootUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var user = new IAUser
            {
                Id = 1,
                Username = "admin",
                FullName = "Quản trị viên tối cao",
                Email = "admin@solaris.vn",
                PhoneNumber = "0900000000",
                CitizenId = "000000000001",
                PasswordHash = "hash",
                IsActive = true
            };
            context.IAUsers.Add(user);
            await context.SaveChangesAsync();

            var userService = new IAUserService(context, mapper);

            // Act & Assert
            var act = async () => await userService.ToggleActiveAsync(1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể tạm khóa tài khoản quản trị viên tối cao*");
        }
        #endregion
    }
}
