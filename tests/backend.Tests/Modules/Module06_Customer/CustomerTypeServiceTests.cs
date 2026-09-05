using AutoMapper;
using backend.DTOs.CustomerTypeDTOs;
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
    /// MODULE 6: CUSTOMER MASTER DATA
    /// UNIT TEST: CustomerTypeService (Quản lý Phân loại Khách hàng)
    /// ============================================================================
    /// </summary>
    public class CustomerTypeServiceTests
    {
        private readonly IMapper _mapper;

        public CustomerTypeServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM THEO TỪ KHÓA / TÊN / TRẠNG THÁI
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo từ khóa (Code, Name), lọc theo danh sách tên và trạng thái IsActive.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.AddRange(
                new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ", IsActive = true },
                new CustomerType { Id = 2, Code = "LE", Name = "Khách lẻ", IsActive = true },
                new CustomerType { Id = 3, Code = "HORECA", Name = "Khách sạn Nhà hàng", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);

            // Act 1: Tìm kiếm từ khóa "sỉ"
            var searchResult = await service.GetPagedAsync("sỉ", null, null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Act 3: Lọc theo danh sách tên "Khách lẻ"
            var nameResult = await service.GetPagedAsync(null, "Khách lẻ", null, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("SI");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("HORECA");

            nameResult.TotalRecords.Should().Be(1);
            nameResult.Items.First().Code.Should().Be("LE");
        }
        #endregion

        #region TC02: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC02: Lấy chi tiết phân loại theo Id hợp lệ và trả về null nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrNull()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.Add(new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ", Description = "Bán buôn số lượng lớn" });
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);

            // Act
            var validResult = await service.GetByIdAsync(1);
            var invalidResult = await service.GetByIdAsync(999);

            // Assert
            validResult.Should().NotBeNull();
            validResult!.Code.Should().Be("SI");
            validResult.Name.Should().Be("Khách sỉ");
            validResult.Description.Should().Be("Bán buôn số lượng lớn");

            invalidResult.Should().BeNull();
        }
        #endregion

        #region TC03: TẠO MỚI PHÂN LOẠI THÀNH CÔNG (TỰ ĐỘNG CHUẨN HÓA CODE)
        /// <summary>
        /// TC03: Tạo mới phân loại thành công, tự động viết hoa Code và cắt tỉa khoảng trắng.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new CustomerTypeService(context, _mapper);

            var dto = new CustomerTypeCreateDto
            {
                Code = "  agency  ",
                Name = "  Đại lý cấp 1  ",
                Description = "  Đại lý phân phối độc quyền  ",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.CustomerTypes.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("AGENCY");
            createdEntity.Name.Should().Be("Đại lý cấp 1");
            createdEntity.Description.Should().Be("Đại lý phân phối độc quyền");
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC04: CHẶN TRÙNG MÃ CODE (CASE-INSENSITIVE)
        /// <summary>
        /// TC04: Ném InvalidOperationException khi cố tình tạo phân loại có mã Code bị trùng lặp.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.Add(new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ" });
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);
            var duplicateDto = new CustomerTypeCreateDto { Code = "si", Name = "Khách sỉ cấp 2" };

            // Act & Assert
            var act = () => service.CreateAsync(duplicateDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã phân loại 'si' đã tồn tại trên hệ thống.*");
        }
        #endregion

        #region TC05: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC05: Cập nhật thông tin phân loại thành công, chặn cập nhật nếu mã Code bị trùng với bản ghi khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.AddRange(
                new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ" },
                new CustomerType { Id = 2, Code = "LE", Name = "Khách lẻ" }
            );
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new CustomerTypeUpdateDto { Code = "SI_VIP", Name = "Khách sỉ VIP", IsActive = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với bản ghi ID = 2
            var duplicateDto = new CustomerTypeUpdateDto { Code = "LE", Name = "Tên bất kỳ", IsActive = true };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updatedEntity = await context.CustomerTypes.FindAsync(1);
            updatedEntity!.Code.Should().Be("SI_VIP");
            updatedEntity.Name.Should().Be("Khách sỉ VIP");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã phân loại 'LE' đã được sử dụng bởi phân loại khác.*");
        }
        #endregion

        #region TC06: SAFETY SHIELD - CHẶN XÓA KHI CÓ KHÁCH HÀNG LIÊN KẾT
        /// <summary>
        /// TC06: Đảm bảo tính toàn vẹn dữ liệu: Chặn xóa phân loại nếu đang có Khách hàng liên kết.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasLinkedCustomers()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var customerType = new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ" };
            context.CustomerTypes.Add(customerType);
            context.Customers.Add(new Customer
            {
                Id = 10,
                Code = "KH001",
                Name = "Công ty Đại Phát",
                PhoneNumber = "0901234567",
                CustomerTypeId = 1,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa phân loại này vì đang có Khách hàng liên kết.*");
        }
        #endregion

        #region TC07: XÓA THÀNH CÔNG KHI KHÔNG CÓ RÀNG BUỘC
        /// <summary>
        /// TC07: Xóa thành công phân loại độc lập không có dữ liệu ràng buộc (Soft Delete).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldDeleteSuccessfully_WhenNoConstraints()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.Add(new CustomerType { Id = 1, Code = "TEMP", Name = "Phân loại tạm" });
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedEntity = await context.CustomerTypes.FindAsync(1);
            deletedEntity!.IsDeleted.Should().BeTrue();
            deletedEntity.DeletedAt.Should().NotBeNull();
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
            context.CustomerTypes.Add(new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ", IsActive = true });
            await context.SaveChangesAsync();

            var service = new CustomerTypeService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.CustomerTypes.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
