using AutoMapper;
using backend.DTOs.CustomerGroupDTOs;
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
    /// 🧪 UNIT TEST: CustomerGroupService (Quản lý Nhóm Khách hàng / Marketing Tag)
    /// ============================================================================
    /// </summary>
    public class CustomerGroupServiceTests
    {
        private readonly IMapper _mapper;

        public CustomerGroupServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: LẤY TẤT CẢ DANH SÁCH & LỌC THEO ISACTIVEONLY
        /// <summary>
        /// TC01: Kiểm tra lấy toàn bộ danh sách nhóm và lọc theo isActiveOnly.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldFilterIsActiveOnlyCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.AddRange(
                new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP", IsActive = true },
                new CustomerGroup { Id = 2, Code = "POTENTIAL", Name = "Khách tiềm năng", IsActive = true },
                new CustomerGroup { Id = 3, Code = "INACTIVE", Name = "Khách ngừng giao dịch", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);

            // Act
            var allResult = await service.GetAllListAsync(false);
            var activeOnlyResult = await service.GetAllListAsync(true);

            // Assert
            allResult.Should().HaveCount(3);
            activeOnlyResult.Should().HaveCount(2);
            activeOnlyResult.Should().NotContain(x => x.Code == "INACTIVE");
        }
        #endregion

        #region TC02: PHÂN TRANG & TÌM KIẾM THEO TỪ KHÓA / TÊN / TRẠNG THÁI
        /// <summary>
        /// TC02: Kiểm tra tìm kiếm paged theo từ khóa, tên, trạng thái và thời gian.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.AddRange(
                new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách Hàng VIP", IsActive = true },
                new CustomerGroup { Id = 2, Code = "LOYAL", Name = "Khách Hàng Thân Thiết", IsActive = true },
                new CustomerGroup { Id = 3, Code = "NEW", Name = "Khách Hàng Mới", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "Thân Thiết"
            var searchResult = await service.GetPagedAsync("Thân Thiết", null, null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Act 3: Lọc theo danh sách tên
            var nameResult = await service.GetPagedAsync(null, "Khách Hàng VIP", null, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("LOYAL");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("NEW");

            nameResult.TotalRecords.Should().Be(1);
            nameResult.Items.First().Code.Should().Be("VIP");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết nhóm khách hàng theo ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrNull()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.Add(new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP", Description = "Top 10% chi tiêu" });
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);

            // Act
            var validResult = await service.GetByIdAsync(1);
            var invalidResult = await service.GetByIdAsync(999);

            // Assert
            validResult.Should().NotBeNull();
            validResult!.Code.Should().Be("VIP");
            validResult.Name.Should().Be("Khách VIP");
            validResult.Description.Should().Be("Top 10% chi tiêu");

            invalidResult.Should().BeNull();
        }
        #endregion

        #region TC04: TẠO MỚI NHÓM THÀNH CÔNG (CHUẨN HÓA CODE)
        /// <summary>
        /// TC04: Tạo mới nhóm khách hàng thành công, tự động viết hoa Code.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new CustomerGroupService(context, _mapper);

            var dto = new CustomerGroupCreateDto
            {
                Code = "  b2b_corp  ",
                Name = "  Doanh nghiệp B2B  ",
                Description = "  Các tập đoàn lớn  ",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.CustomerGroups.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("B2B_CORP");
            createdEntity.Name.Should().Be("Doanh nghiệp B2B");
            createdEntity.Description.Should().Be("Các tập đoàn lớn");
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC05: CHẶN TRÙNG MÃ CODE
        /// <summary>
        /// TC05: Ném InvalidOperationException khi tạo nhóm có mã Code trùng lặp.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.Add(new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP" });
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);
            var duplicateDto = new CustomerGroupCreateDto { Code = "vip", Name = "Nhóm VIP 2" };

            // Act & Assert
            var act = () => service.CreateAsync(duplicateDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã nhóm khách hàng 'vip' đã tồn tại trên hệ thống.*");
        }
        #endregion

        #region TC06: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC06: Cập nhật nhóm thành công, chặn trùng lặp mã với nhóm khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.AddRange(
                new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP" },
                new CustomerGroup { Id = 2, Code = "LOYAL", Name = "Khách Thân Thiết" }
            );
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new CustomerGroupUpdateDto { Code = "VIP_GOLD", Name = "Khách VIP Vàng", IsActive = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với ID = 2
            var duplicateDto = new CustomerGroupUpdateDto { Code = "LOYAL", Name = "Tên bất kỳ", IsActive = true };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updatedEntity = await context.CustomerGroups.FindAsync(1);
            updatedEntity!.Code.Should().Be("VIP_GOLD");
            updatedEntity.Name.Should().Be("Khách VIP Vàng");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã nhóm 'LOYAL' đã bị trùng lặp với dữ liệu khác.*");
        }
        #endregion

        #region TC07: XÓA NHÓM & TỰ ĐỘNG DỌN DẸP CUSTOMERGROUPLINKS
        /// <summary>
        /// TC07: Xóa nhóm và tự động dọn dẹp các bản ghi liên kết CustomerGroupLink liên quan.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldDeleteAndCleanUpCustomerGroupLinks()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var group = new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP" };
            context.CustomerGroups.Add(group);
            context.CustomerGroupLinks.Add(new CustomerGroupLink
            {
                CustomerId = 10,
                CustomerGroupId = 1,
                AssignedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            var deletedGroup = await context.CustomerGroups.FindAsync(1);
            deletedGroup!.IsDeleted.Should().BeTrue();

            var remainingLinks = await context.CustomerGroupLinks.Where(l => l.CustomerGroupId == 1).ToListAsync();
            remainingLinks.Should().BeEmpty();
        }
        #endregion

        #region TC08: CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC08: Đổi trạng thái hoạt động (true -> false) thành công.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleStatusCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.Add(new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP", IsActive = true });
            await context.SaveChangesAsync();

            var service = new CustomerGroupService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.CustomerGroups.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
