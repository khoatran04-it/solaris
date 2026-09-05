using AutoMapper;
using backend.DTOs.SupplierTypeDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module03_Supplier
{
    /// <summary>
    /// ============================================================================
    /// MODULE 3: SUPPLIER MASTER DATA
    /// UNIT TEST: SupplierTypeService (Quản lý Phân loại Nhà cung cấp)
    /// ============================================================================
    /// </summary>
    public class SupplierTypeServiceTests
    {
        private readonly IMapper _mapper;

        public SupplierTypeServiceTests()
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
            context.SupplierTypes.AddRange(
                new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn / Nông trại", IsActive = true },
                new SupplierType { Id = 2, Code = "COOP", Name = "Hợp tác xã", IsActive = true },
                new SupplierType { Id = 3, Code = "IMPORT", Name = "Công ty Nhập khẩu", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "vườn"
            var searchResult = await service.GetPagedAsync("vườn", null, null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Act 3: Lọc theo danh sách tên "Hợp tác xã"
            var nameResult = await service.GetPagedAsync(null, "Hợp tác xã", null, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("FARM");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("IMPORT");

            nameResult.TotalRecords.Should().Be(1);
            nameResult.Items.First().Code.Should().Be("COOP");
        }
        #endregion

        #region TC02: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC02: Lấy chi tiết phân loại theo Id hợp lệ và ném KeyNotFoundException nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrNull()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierTypes.Add(new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn", Description = "Nông dân trực tiếp canh tác" });
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            // Act
            var validResult = await service.GetByIdAsync(1);
            var invalidResult = await service.GetByIdAsync(999);

            // Assert
            validResult.Should().NotBeNull();
            validResult!.Code.Should().Be("FARM");
            validResult.Name.Should().Be("Nhà vườn");
            validResult.Description.Should().Be("Nông dân trực tiếp canh tác");

            invalidResult.Should().BeNull();
        }
        #endregion

        #region TC03: TẠO MỚI PHÂN LOẠI THÀNH CÔNG (TỰ ĐỘNG CHUẨN HÓA CODE & NAME)
        /// <summary>
        /// TC03: Tạo mới phân loại thành công, tự động viết hoa Code và cắt tỉa khoảng trắng.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new SupplierTypeService(context, _mapper);

            var dto = new SupplierTypeCreateDto
            {
                Code = "  distributor  ",
                Name = "  Nhà phân phối sỉ  ",
                Description = "  Cung ứng số lượng lớn  ",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.SupplierTypes.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("DISTRIBUTOR");
            createdEntity.Name.Should().Be("Nhà phân phối sỉ");
            createdEntity.Description.Should().Be("Cung ứng số lượng lớn");
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC04: CHẶN TRÙNG MÃ CODE & TÊN PHÂN LOẠI
        /// <summary>
        /// TC04: Ném InvalidOperationException khi cố tình tạo phân loại có mã Code hoặc Tên bị trùng lặp.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCodeOrName()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierTypes.Add(new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn" });
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            var duplicateCodeDto = new SupplierTypeCreateDto { Code = "farm", Name = "Trang trại khác" };
            var duplicateNameDto = new SupplierTypeCreateDto { Code = "NEW_CODE", Name = "  Nhà Vườn  " };

            // Act & Assert 1: Trùng mã
            var actCode = () => service.CreateAsync(duplicateCodeDto);
            await actCode.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã phân loại 'farm' đã tồn tại.*");

            // Act & Assert 2: Trùng tên
            var actName = () => service.CreateAsync(duplicateNameDto);
            await actName.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tên phân loại '  Nhà Vườn  ' đã tồn tại.*");
        }
        #endregion

        #region TC05: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC05: Cập nhật thông tin phân loại thành công, chặn cập nhật nếu mã Code/Name bị trùng với bản ghi khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicate()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierTypes.AddRange(
                new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn" },
                new SupplierType { Id = 2, Code = "COOP", Name = "Hợp tác xã" }
            );
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new SupplierTypeUpdateDto { Code = "FARM_LOCAL", Name = "Nhà vườn nội địa", IsActive = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với bản ghi ID = 2
            var duplicateDto = new SupplierTypeUpdateDto { Code = "COOP", Name = "Tên bất kỳ", IsActive = true };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updatedEntity = await context.SupplierTypes.FindAsync(1);
            updatedEntity!.Code.Should().Be("FARM_LOCAL");
            updatedEntity.Name.Should().Be("Nhà vườn nội địa");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã phân loại 'COOP' đã bị trùng lặp*");
        }
        #endregion

        #region TC06: SAFETY SHIELD - CHẶN XÓA KHI CÓ NHÀ CUNG CẤP TRỰC THUỘC
        /// <summary>
        /// TC06: Đảm bảo tính toàn vẹn dữ liệu: Chặn xóa phân loại nếu đang có Nhà cung cấp liên kết.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasSuppliers()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var supplierType = new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn" };
            context.SupplierTypes.Add(supplierType);
            context.Suppliers.Add(new Supplier
            {
                Id = 10,
                Code = "NCC01",
                Name = "Vườn Trái Cây Chín Bảnh",
                Phone = "0901234567",
                Email = "chinbanh@gmail.com",
                SupplierTypeId = 1,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa phân loại này vì đang có nhà cung cấp trực thuộc.*");
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
            context.SupplierTypes.Add(new SupplierType { Id = 1, Code = "TEST", Name = "Phân loại thử nghiệm" });
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedEntity = await context.SupplierTypes.FindAsync(1);
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
            context.SupplierTypes.Add(new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn", IsActive = true });
            await context.SaveChangesAsync();

            var service = new SupplierTypeService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.SupplierTypes.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
