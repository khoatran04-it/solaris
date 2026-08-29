using AutoMapper;
using backend.DTOs.CustomerTierDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module06_Customer
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 6: CUSTOMER MASTER DATA
    /// 🧪 UNIT TEST: CustomerTierService (Quản lý Bậc Hạng & Chiết Khấu Thành Viên)
    /// ============================================================================
    /// </summary>
    public class CustomerTierServiceTests
    {
        private readonly IMapper _mapper;

        public CustomerTierServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: LẤY DANH SÁCH TẤT CẢ SẮP XẾP THEO MINSPENDING
        /// <summary>
        /// TC01: Lấy danh sách tất cả bậc hạng được sắp xếp tăng dần theo MinSpending.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldOrderByMinSpending()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.AddRange(
                new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng", MinSpending = 10000000, DiscountPercent = 5 },
                new CustomerTier { Id = 2, Code = "BRONZE", Name = "Hạng Đồng", MinSpending = 1000000, DiscountPercent = 1 },
                new CustomerTier { Id = 3, Code = "SILVER", Name = "Hạng Bạc", MinSpending = 5000000, DiscountPercent = 3 }
            );
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act
            var result = (await service.GetAllListAsync()).ToList();

            // Assert
            result.Should().HaveCount(3);
            result[0].Code.Should().Be("BRONZE");
            result[1].Code.Should().Be("SILVER");
            result[2].Code.Should().Be("GOLD");
        }
        #endregion

        #region TC02: PHÂN TRANG & TÌM KIẾM THEO TỪ KHÓA / TÊN / TRẠNG THÁI
        /// <summary>
        /// TC02: Kiểm tra tìm kiếm paged theo từ khóa, tên và trạng thái.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.AddRange(
                new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng", IsActive = true },
                new CustomerTier { Id = 2, Code = "SILVER", Name = "Hạng Bạc", IsActive = true },
                new CustomerTier { Id = 3, Code = "DIAMOND", Name = "Hạng Kim Cương", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "Kim Cương"
            var searchResult = await service.GetPagedAsync("Kim Cương", null, null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Act 3: Lọc theo danh sách tên
            var nameResult = await service.GetPagedAsync(null, "Hạng Vàng", null, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("DIAMOND");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("DIAMOND");

            nameResult.TotalRecords.Should().Be(1);
            nameResult.Items.First().Code.Should().Be("GOLD");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết bậc hạng theo ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrNull()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.Add(new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng", MinSpending = 10000000, DiscountPercent = 5 });
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act
            var validResult = await service.GetByIdAsync(1);
            var invalidResult = await service.GetByIdAsync(999);

            // Assert
            validResult.Should().NotBeNull();
            validResult!.Code.Should().Be("GOLD");
            validResult.DiscountPercent.Should().Be(5);
            validResult.MinSpending.Should().Be(10000000);

            invalidResult.Should().BeNull();
        }
        #endregion

        #region TC04: TẠO MỚI BẬC HẠNG THÀNH CÔNG
        /// <summary>
        /// TC04: Tạo mới bậc hạng thành công với mức chiết khấu và hạn mức chi tiêu.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithDiscountAndSpending()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new CustomerTierService(context, _mapper);

            var dto = new CustomerTierCreateDto
            {
                Code = "  platinum  ",
                Name = "  Hạng Bạch Kim  ",
                DiscountPercent = 7.5m,
                MinSpending = 20000000,
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.CustomerTiers.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("PLATINUM");
            createdEntity.Name.Should().Be("Hạng Bạch Kim");
            createdEntity.DiscountPercent.Should().Be(7.5m);
            createdEntity.MinSpending.Should().Be(20000000);
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC05: CHẶN TRÙNG MÃ BẬC HẠNG
        /// <summary>
        /// TC05: Ném InvalidOperationException khi tạo bậc hạng có mã Code trùng lặp.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.Add(new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng" });
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);
            var duplicateDto = new CustomerTierCreateDto { Code = "gold", Name = "Hạng Vàng 2" };

            // Act & Assert
            var act = () => service.CreateAsync(duplicateDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã bậc hạng 'gold' đã tồn tại trên hệ thống.*");
        }
        #endregion

        #region TC06: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC06: Cập nhật bậc hạng thành công, chặn trùng lặp mã với bậc hạng khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.AddRange(
                new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng" },
                new CustomerTier { Id = 2, Code = "SILVER", Name = "Hạng Bạc" }
            );
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new CustomerTierUpdateDto { Code = "GOLD_PLUS", Name = "Hạng Vàng Cao Cấp", DiscountPercent = 6, MinSpending = 12000000, IsActive = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với ID = 2
            var duplicateDto = new CustomerTierUpdateDto { Code = "SILVER", Name = "Tên bất kỳ", IsActive = true };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updatedEntity = await context.CustomerTiers.FindAsync(1);
            updatedEntity!.Code.Should().Be("GOLD_PLUS");
            updatedEntity.DiscountPercent.Should().Be(6);

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã bậc hạng 'SILVER' đã được sử dụng bởi bậc hạng khác.*");
        }
        #endregion

        #region TC07: SAFETY SHIELD - CHẶN XÓA KHI CÓ KHÁCH HÀNG THUỘC HẠNG NÀY
        /// <summary>
        /// TC07: Đảm bảo an toàn dữ liệu: Chặn xóa bậc hạng nếu đang có khách hàng thuộc hạng.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasLinkedCustomers()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var tier = new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng" };
            context.CustomerTiers.Add(tier);
            context.Customers.Add(new Customer
            {
                Id = 10,
                Code = "KH001",
                Name = "Khách Hàng Vip",
                PhoneNumber = "0911222333",
                CustomerTierId = 1,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa bậc hạng này vì đang có Khách hàng thuộc hạng này.*");
        }
        #endregion

        #region TC08: XÓA THÀNH CÔNG KHI KHÔNG CÓ RÀNG BUỘC
        /// <summary>
        /// TC08: Xóa thành công bậc hạng độc lập.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldDeleteSuccessfully_WhenNoConstraints()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.Add(new CustomerTier { Id = 1, Code = "TEST", Name = "Hạng thử nghiệm" });
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedEntity = await context.CustomerTiers.FindAsync(1);
            deletedEntity!.IsDeleted.Should().BeTrue();
            deletedEntity.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC09: CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC09: Đổi trạng thái hoạt động của bậc hạng thành công.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleStatusCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTiers.Add(new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng", IsActive = true });
            await context.SaveChangesAsync();

            var service = new CustomerTierService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.CustomerTiers.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
