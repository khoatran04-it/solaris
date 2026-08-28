using AutoMapper;
using backend.DTOs.SupplierAddressDTOs;
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
    /// 📦 MODULE 3: SUPPLIER MASTER DATA
    /// 🧪 UNIT TEST: SupplierAddressService (Quản lý Địa chỉ kho Nhà cung cấp)
    /// ============================================================================
    /// </summary>
    public class SupplierAddressServiceTests
    {
        private readonly IMapper _mapper;

        public SupplierAddressServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: LẤY DANH SÁCH ĐỊA CHỈ THEO SUPPLIER ID (DEFAULT ĐỨNG ĐẦU)
        /// <summary>
        /// TC01: Lấy danh sách địa chỉ của nhà cung cấp, đảm bảo địa chỉ IsDefault = true luôn được xếp lên đầu.
        /// </summary>
        [Fact]
        public async Task GetBySupplierIdAsync_ShouldPrioritizeDefaultAddress()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.SupplierAddresses.AddRange(
                new SupplierAddress
                {
                    Id = 1,
                    SupplierId = 1,
                    ContactName = "Kho Phụ",
                    ContactPhone = "0901",
                    StreetAddress = "10 Đường A",
                    Ward = "Phường 1",
                    District = "Quận 1",
                    Province = "TP. HCM",
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new SupplierAddress
                {
                    Id = 2,
                    SupplierId = 1,
                    ContactName = "Kho Chính (Mặc định)",
                    ContactPhone = "0902",
                    StreetAddress = "20 Đường B",
                    Ward = "Phường 2",
                    District = "Quận 2",
                    Province = "TP. HCM",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            );
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            // Act
            var addresses = (await service.GetBySupplierIdAsync(1)).ToList();

            // Assert
            addresses.Should().HaveCount(2);
            addresses.First().Id.Should().Be(2); // Kho Chính mặc định phải đứng đầu
            addresses.First().IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC02: ĐẶC THÙ - THÊM ĐỊA CHỈ ĐẦU TIÊN TỰ ĐỘNG LÀ MẶC ĐỊNH
        /// <summary>
        /// TC02: Khi nhà cung cấp chưa có địa chỉ nào, thêm địa chỉ mới (dù truyền IsDefault = false) vẫn tự động gán IsDefault = true.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldAutomaticallySetDefault_ForFirstAddress()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            var createDto = new SupplierAddressCreateDto
            {
                ContactName = "Kho Số 1",
                ContactPhone = "0901234567",
                StreetAddress = "123 Lê Lợi",
                Ward = "Bến Nghé",
                District = "Quận 1",
                Province = "Hồ Chí Minh",
                IsDefault = false // Truyền false
            };

            // Act
            var addressId = await service.CreateAsync(1, createDto);

            // Assert
            addressId.Should().BeGreaterThan(0);
            var entity = await context.SupplierAddresses.FindAsync(addressId);
            entity.Should().NotBeNull();
            entity!.IsDefault.Should().BeTrue(); // Đã tự động kích hoạt mặc định
        }
        #endregion

        #region TC03: ĐẶC THÙ - THÊM ĐỊA CHỈ DEFAULT MỚI SẼ TẮT DEFAULT CỦA ĐỊA CHỈ CŨ
        /// <summary>
        /// TC03: Thêm một địa chỉ mới và đặt IsDefault = true -> Hệ thống tự động tắt cờ IsDefault của địa chỉ mặc định trước đó.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldTransferDefault_WhenNewAddressIsDefault()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.SupplierAddresses.Add(new SupplierAddress
            {
                Id = 1,
                SupplierId = 1,
                ContactName = "Kho Cũ",
                ContactPhone = "0901",
                StreetAddress = "1 Đường Cũ",
                Ward = "Phường Cũ",
                District = "Quận Cũ",
                Province = "Tỉnh Cũ",
                IsDefault = true
            });
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            var createDto = new SupplierAddressCreateDto
            {
                ContactName = "Kho Mới Toanh",
                ContactPhone = "0999999999",
                StreetAddress = "99 Đường Mới",
                Ward = "Phường Mới",
                District = "Quận Mới",
                Province = "Tỉnh Mới",
                IsDefault = true // Đặt làm mặc định mới
            };

            // Act
            var newId = await service.CreateAsync(1, createDto);

            // Assert
            var oldAddress = await context.SupplierAddresses.FindAsync(1);
            var newAddress = await context.SupplierAddresses.FindAsync(newId);

            oldAddress!.IsDefault.Should().BeFalse();
            newAddress!.IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC04: CẬP NHẬT ĐỊA CHỈ VÀ LUÂN CHUYỂN QUYỀN MẶC ĐỊNH
        /// <summary>
        /// TC04: Cập nhật một địa chỉ thường thành mặc định -> Địa chỉ mặc định cũ bị tắt.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldSwitchDefaultAddress_WhenUpdatedToDefault()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.SupplierAddresses.AddRange(
                new SupplierAddress { Id = 1, SupplierId = 1, ContactName = "Kho 1 (Default)", ContactPhone = "0901", StreetAddress = "A", Ward = "B", District = "C", Province = "D", IsDefault = true },
                new SupplierAddress { Id = 2, SupplierId = 1, ContactName = "Kho 2 (Normal)", ContactPhone = "0902", StreetAddress = "E", Ward = "F", District = "G", Province = "H", IsDefault = false }
            );
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            // Act: Biến Kho 2 thành mặc định
            var updateDto = new SupplierAddressUpdateDto
            {
                ContactName = "Kho 2 (Đã thành Default)",
                ContactPhone = "0902",
                StreetAddress = "E",
                Ward = "F",
                District = "G",
                Province = "H",
                IsDefault = true
            };
            var result = await service.UpdateAsync(2, updateDto);

            // Assert
            result.Should().BeTrue();
            var addr1 = await context.SupplierAddresses.FindAsync(1);
            var addr2 = await context.SupplierAddresses.FindAsync(2);

            addr1!.IsDefault.Should().BeFalse();
            addr2!.IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC05: ĐẶC THÙ - KHÔNG CHO PHÉP TẮT MẶC ĐỊNH KHI CHỈ CÓ DUY NHẤT 1 ĐỊA CHỈ
        /// <summary>
        /// TC05: Nếu chỉ có 1 địa chỉ duy nhất, cố tình update IsDefault = false thì hệ thống vẫn ép giữ nguyên IsDefault = true.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldKeepDefault_WhenOnlyOneAddressExists()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.SupplierAddresses.Add(
                new SupplierAddress { Id = 1, SupplierId = 1, ContactName = "Kho Duy Nhất", ContactPhone = "0901", StreetAddress = "A", Ward = "B", District = "C", Province = "D", IsDefault = true }
            );
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            var updateDto = new SupplierAddressUpdateDto
            {
                ContactName = "Kho Duy Nhất",
                ContactPhone = "0901",
                StreetAddress = "A",
                Ward = "B",
                District = "C",
                Province = "D",
                IsDefault = false // Cố tình tắt mặc định
            };

            // Act
            await service.UpdateAsync(1, updateDto);

            // Assert
            var entity = await context.SupplierAddresses.FindAsync(1);
            entity!.IsDefault.Should().BeTrue(); // Vẫn là mặc định
        }
        #endregion

        #region TC06: ĐẶC THÙ - XÓA ĐỊA CHỈ DEFAULT TỰ ĐỘNG CHUYỂN QUYỀN CHO ĐỊA CHỈ KHÁC
        /// <summary>
        /// TC06: Khi xóa địa chỉ đang là mặc định, hệ thống tự động chọn địa chỉ còn lại cũ nhất để làm mặc định thay thế.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldTransferDefault_ToNextAddress_WhenDefaultIsDeleted()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.SupplierAddresses.AddRange(
                new SupplierAddress { Id = 1, SupplierId = 1, ContactName = "Kho 1 (Default)", ContactPhone = "0901", StreetAddress = "A", Ward = "B", District = "C", Province = "D", IsDefault = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new SupplierAddress { Id = 2, SupplierId = 1, ContactName = "Kho 2", ContactPhone = "0902", StreetAddress = "E", Ward = "F", District = "G", Province = "H", IsDefault = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            );
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            // Act: Xóa Kho 1 (đang là Default)
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedAddr = await context.SupplierAddresses.FindAsync(1);
            var remainingAddr = await context.SupplierAddresses.FindAsync(2);

            deletedAddr!.IsDeleted.Should().BeTrue();
            remainingAddr!.IsDefault.Should().BeTrue(); // Đã được thừa kế làm Default
        }
        #endregion

        #region TC07: ĐẶC THÙ - CHỈ ĐỊNH THỦ CÔNG ĐỊA CHỈ MẶC ĐỊNH (SET DEFAULT)
        /// <summary>
        /// TC07: Gọi SetDefaultAsync thành công cho một địa chỉ trực thuộc nhà cung cấp.
        /// </summary>
        [Fact]
        public async Task SetDefaultAsync_ShouldSetDefaultCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.SupplierAddresses.AddRange(
                new SupplierAddress { Id = 1, SupplierId = 1, ContactName = "Kho 1", ContactPhone = "0901", StreetAddress = "A", Ward = "B", District = "C", Province = "D", IsDefault = true },
                new SupplierAddress { Id = 2, SupplierId = 1, ContactName = "Kho 2", ContactPhone = "0902", StreetAddress = "E", Ward = "F", District = "G", Province = "H", IsDefault = false }
            );
            await context.SaveChangesAsync();

            var service = new SupplierAddressService(context, _mapper);

            // Act: Đặt Kho 2 làm Default
            var result = await service.SetDefaultAsync(2, 1);

            // Assert
            result.Should().BeTrue();
            var addr1 = await context.SupplierAddresses.FindAsync(1);
            var addr2 = await context.SupplierAddresses.FindAsync(2);

            addr1!.IsDefault.Should().BeFalse();
            addr2!.IsDefault.Should().BeTrue();
        }
        #endregion
    }
}
