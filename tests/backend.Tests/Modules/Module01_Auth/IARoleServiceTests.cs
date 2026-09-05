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
    /// 🧪 UNIT TEST: IARoleService (Quản lý Vai trò & Cấu hình Ma trận Quyền hạn)
    /// ============================================================================
    /// </summary>
    public class IARoleServiceTests
    {
        #region TEST CASE 01: PHÂN TRANG VÀ TÌM KIẾM VAI TRÒ
        /// <summary>
        /// TC01: Phân trang danh sách vai trò kèm bộ lọc tìm kiếm theo từ khóa (Search Keyword).
        /// </summary>
        [Fact(DisplayName = "TC01 - Phân trang và tìm kiếm vai trò theo từ khóa")]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            context.IARoles.AddRange(
                new IARole { Id = 1, Code = "ADMIN", Name = "Quản trị viên hệ thống", IsActive = true },
                new IARole { Id = 2, Code = "SALES_LEAD", Name = "Trưởng phòng kinh doanh", IsActive = true },
                new IARole { Id = 3, Code = "WAREHOUSE_STAFF", Name = "Nhân viên thủ kho", IsActive = false }
            );
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);

            // Act: Tìm kiếm từ khóa "kinh doanh"
            var result = await roleService.GetPagedAsync("kinh doanh", null, 1, 10);

            // Assert
            result.TotalRecords.Should().Be(1);
            result.Items.Should().ContainSingle(r => r.Code == "SALES_LEAD");
        }
        #endregion

        #region TEST CASE 02: LẤY CHI TIẾT VAI TRÒ KÈM DANH SÁCH QUYỀN
        /// <summary>
        /// TC02: Lấy thông tin chi tiết của 1 vai trò dựa vào ID, trả về danh sách ID các quyền thuộc vai trò.
        /// </summary>
        [Fact(DisplayName = "TC02 - Lấy chi tiết vai trò theo ID kèm danh sách quyền hạn")]
        public async Task GetByIdAsync_ShouldReturnRoleWithPermissionIds()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var perm1 = new IAPermission { Id = 10, Module = "Kho", Code = "WH_VIEW", Name = "Xem kho" };
            var perm2 = new IAPermission { Id = 11, Module = "Kho", Code = "WH_MANAGE", Name = "Quản lý kho" };
            context.IAPermissions.AddRange(perm1, perm2);

            var role = new IARole { Id = 5, Code = "STORE_KEEPER", Name = "Thủ kho", Description = "Phụ trách kho" };
            context.IARoles.Add(role);

            context.IARolePermissions.AddRange(
                new IARolePermission { RoleId = 5, PermissionId = 10 },
                new IARolePermission { RoleId = 5, PermissionId = 11 }
            );
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);

            // Act
            var result = await roleService.GetByIdAsync(5);

            // Assert
            result.Should().NotBeNull();
            result!.Code.Should().Be("STORE_KEEPER");
            result.PermissionIds.Should().BeEquivalentTo(new[] { 10, 11 });
        }
        #endregion

        #region TEST CASE 03: CHẶN TRÙNG MÃ VAI TRÒ (CODE)
        /// <summary>
        /// TC03: Khi tạo vai trò với mã Code đã tồn tại (không phân biệt hoa/thường),
        /// hệ thống phải từ chối và ném InvalidOperationException.
        /// </summary>
        [Fact(DisplayName = "TC03 - Tạo vai trò thất bại khi trùng Mã vai trò (Code không phân biệt hoa/thường)")]
        public async Task CreateAsync_WithDuplicateCode_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            context.IARoles.Add(new IARole { Code = "ADMIN", Name = "Quản trị viên" });
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);
            var createDto = new IARoleCreateDto
            {
                Code = "admin", // Trùng chữ thường với chữ HOA ADMIN trong DB
                Name = "Quản trị viên 2"
            };

            // Act & Assert
            var act = async () => await roleService.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại trong hệ thống*");
        }
        #endregion

        #region TEST CASE 04: TẠO VAI TRÒ THÀNH CÔNG KÈM QUYỀN HẠN
        /// <summary>
        /// TC04: Tạo vai trò mới thành công, tự động chuẩn hóa Code in hoa và gán quyền vào IARolePermissions.
        /// </summary>
        [Fact(DisplayName = "TC04 - Tạo vai trò thành công kèm danh sách quyền hạn ban đầu")]
        public async Task CreateAsync_WithValidData_ShouldCreateRoleAndAssignPermissions()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var p1 = new IAPermission { Id = 10, Module = "Đơn hàng", Code = "ORDER_VIEW", Name = "Xem đơn" };
            var p2 = new IAPermission { Id = 11, Module = "Đơn hàng", Code = "ORDER_CREATE", Name = "Tạo đơn" };
            context.IAPermissions.AddRange(p1, p2);
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);
            var createDto = new IARoleCreateDto
            {
                Code = "sales_staff", // Sẽ được chuẩn hóa thành SALES_STAFF
                Name = "Nhân viên kinh doanh",
                Description = "Phụ trách bán hàng",
                PermissionIds = new List<int> { 10, 11 }
            };

            // Act
            var newRoleId = await roleService.CreateAsync(createDto);

            // Assert
            newRoleId.Should().BeGreaterThan(0);

            var createdRole = await roleService.GetByIdAsync(newRoleId);
            createdRole.Should().NotBeNull();
            createdRole!.Code.Should().Be("SALES_STAFF");
            createdRole.PermissionIds.Should().HaveCount(2);
            createdRole.PermissionIds.Should().Contain(new[] { 10, 11 });
        }
        #endregion

        #region TEST CASE 05: CẬP NHẬT VÀ ĐỒNG BỘ LẠI QUYỀN HẠN
        /// <summary>
        /// TC05: Cập nhật thông tin vai trò và đồng bộ danh sách quyền:
        /// - Quyền không còn trong danh sách mới sẽ bị xóa bỏ.
        /// - Quyền mới thêm vào sẽ được chèn thêm vào IARolePermissions.
        /// </summary>
        [Fact(DisplayName = "TC05 - Cập nhật vai trò: Đồng bộ lại danh sách quyền hạn (Thêm mới & Xóa bớt)")]
        public async Task UpdateAsync_ShouldSyncRolePermissionsCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var p1 = new IAPermission { Id = 1, Module = "Hệ thống", Code = "P1", Name = "Quyền 1" };
            var p2 = new IAPermission { Id = 2, Module = "Hệ thống", Code = "P2", Name = "Quyền 2" };
            var p3 = new IAPermission { Id = 3, Module = "Hệ thống", Code = "P3", Name = "Quyền 3" };
            context.IAPermissions.AddRange(p1, p2, p3);

            var role = new IARole { Id = 1, Code = "TEST_ROLE", Name = "Tên cũ" };
            context.IARoles.Add(role);

            // Ban đầu role có quyền P1, P2
            context.IARolePermissions.AddRange(
                new IARolePermission { RoleId = 1, PermissionId = 1 },
                new IARolePermission { RoleId = 1, PermissionId = 2 }
            );
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);
            var updateDto = new IARoleUpdateDto
            {
                Name = "Tên mới đã sửa",
                Description = "Mô tả mới",
                IsActive = true,
                PermissionIds = new List<int> { 2, 3 } // Giữ P2, bỏ P1, thêm P3
            };

            // Act
            var isUpdated = await roleService.UpdateAsync(1, updateDto);

            // Assert
            isUpdated.Should().BeTrue();

            var updatedRole = await roleService.GetByIdAsync(1);
            updatedRole!.Name.Should().Be("Tên mới đã sửa");
            updatedRole.PermissionIds.Should().HaveCount(2);
            updatedRole.PermissionIds.Should().BeEquivalentTo(new[] { 2, 3 });
        }
        #endregion

        #region TEST CASE 06: BẬT / TẮT TRẠNG THÁI HOẠT ĐỘNG
        /// <summary>
        /// TC06: Bật/Tắt trạng thái hoạt động (Toggle Active) của vai trò.
        /// </summary>
        [Fact(DisplayName = "TC06 - Đổi trạng thái Hoạt động / Tạm khóa của vai trò (ToggleActive)")]
        public async Task ToggleActiveAsync_ShouldFlipRoleStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var role = new IARole { Id = 8, Code = "TEMP_ROLE", Name = "Vai trò tạm", IsActive = true };
            context.IARoles.Add(role);
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);

            // Act: Khóa vai trò
            var status1 = await roleService.ToggleActiveAsync(8);
            status1.Should().BeFalse();

            // Act: Mở khóa vai trò
            var status2 = await roleService.ToggleActiveAsync(8);
            status2.Should().BeTrue();
        }
        #endregion

        #region TEST CASE 07: XÓA VAI TRÒ
        /// <summary>
        /// TC07: Xóa vai trò khỏi hệ thống.
        /// </summary>
        [Fact(DisplayName = "TC07 - Xóa vai trò thành công")]
        public async Task DeleteAsync_ShouldRemoveRoleFromDatabase()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var role = new IARole { Id = 99, Code = "OBSOLETE_ROLE", Name = "Vai trò lỗi thời" };
            context.IARoles.Add(role);
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);

            // Act
            var isDeleted = await roleService.DeleteAsync(99);

            // Assert (Hệ thống áp dụng Soft Delete)
            isDeleted.Should().BeTrue();
            var roleInDb = await context.IARoles.FindAsync(99);
            roleInDb.Should().NotBeNull();
            roleInDb!.IsDeleted.Should().BeTrue("Vai trò phải được đánh dấu đã xóa (Soft Delete).");

            var act = async () => await roleService.GetByIdAsync(99);
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
        #endregion

        #region TEST CASE 08 & 09: BẢO VỆ VAI TRÒ HỆ THỐNG CỐT LÕI (SUPER ADMIN)
        [Fact(DisplayName = "TC08 - Chặn xóa vai trò quản trị hệ thống mặc định (ADMIN / SUPER_ADMIN)")]
        public async Task DeleteAsync_SuperAdminRole_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var role = new IARole { Id = 1, Code = "SUPER_ADMIN", Name = "Quản trị viên tối cao" };
            context.IARoles.Add(role);
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);

            // Act & Assert
            var act = async () => await roleService.DeleteAsync(1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa vai trò quản trị hệ thống mặc định*");
        }

        [Fact(DisplayName = "TC09 - Chặn tạm khóa vai trò quản trị hệ thống mặc định (ADMIN / SUPER_ADMIN)")]
        public async Task ToggleActiveAsync_SuperAdminRole_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var mapper = TestFactories.CreateAutoMapper();

            var role = new IARole { Id = 1, Code = "ADMIN", Name = "Quản trị viên", IsActive = true };
            context.IARoles.Add(role);
            await context.SaveChangesAsync();

            var roleService = new IARoleService(context, mapper);

            // Act & Assert
            var act = async () => await roleService.ToggleActiveAsync(1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể tạm khóa vai trò quản trị hệ thống mặc định*");
        }
        #endregion
    }
}
