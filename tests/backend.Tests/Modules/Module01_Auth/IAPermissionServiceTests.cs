using backend.Data;
using backend.Enums;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module01_Auth
{
    /// <summary>
    /// ============================================================================
    /// MODULE 1: IDENTITY & ACCESS MANAGEMENT (IAM)
    /// UNIT TEST: IAPermissionService (Đồng bộ danh mục Quyền hạn từ Enum)
    /// ============================================================================
    /// </summary>
    public class IAPermissionServiceTests
    {
        [Fact(DisplayName = "TC01 - Tự động đồng bộ toàn bộ danh mục quyền từ SystemPermission Enum vào Database")]
        public async Task GetAllListAsync_ShouldAutoSyncAllEnumPermissionsIntoDatabase()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new IAPermissionService(context);

            // Act
            var result = await service.GetAllListAsync();

            // Assert
            result.Should().NotBeNull();
            var enumCount = SystemPermissionExtensions.GetAllDefinitions().Count;
            result.Count().Should().Be(enumCount);

            // Kiểm tra trong DB thực tế
            var dbCount = await context.IAPermissions.CountAsync();
            dbCount.Should().Be(enumCount);

            // Kiểm tra các quyền cơ bản tồn tại
            result.Should().Contain(p => p.Code == "ROLE_VIEW" && p.Module == "1. Hệ thống");
            result.Should().Contain(p => p.Code == "ROLE_CREATE");
            result.Should().Contain(p => p.Code == "WAREHOUSE_CREATE" && p.Module == "8. Kho hàng");
            result.Should().Contain(p => p.Code == "RECEIPT_CONFIRM" && p.Module == "10. Vận hành Kho");
        }

        [Fact(DisplayName = "TC02 - Giữ nguyên các quyền đã tồn tại và chỉ chèn thêm quyền còn thiếu")]
        public async Task GetAllListAsync_WithExistingPermissions_ShouldOnlyInsertMissingOnes()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.IAPermissions.Add(new IAPermission
            {
                Id = 1,
                Module = "1. Hệ thống",
                Code = "ROLE_VIEW",
                Name = "Xem danh sách Vai trò"
            });
            await context.SaveChangesAsync();

            var service = new IAPermissionService(context);

            // Act
            var result = await service.GetAllListAsync();

            // Assert
            var enumCount = SystemPermissionExtensions.GetAllDefinitions().Count;
            result.Count().Should().Be(enumCount);

            var existingOne = await context.IAPermissions.FirstOrDefaultAsync(p => p.Code == "ROLE_VIEW");
            existingOne.Should().NotBeNull();
            existingOne!.Id.Should().Be(1, "Quyền đã tồn tại không bị ghi đè hoặc nhân đôi.");
        }
    }
}
