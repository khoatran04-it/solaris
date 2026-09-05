using AutoMapper;
using backend.DTOs.CustomerAddressDTOs;
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
    /// UNIT TEST: CustomerAddressService (Quản lý Sổ Địa Chỉ & Luân Chuyển Mặc Định)
    /// ============================================================================
    /// </summary>
    public class CustomerAddressServiceTests
    {
        private readonly IMapper _mapper;

        public CustomerAddressServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: LẤY SỔ ĐỊA CHỈ - SẮP XẾP DEFAULT LÊN ĐẦU
        /// <summary>
        /// TC01: Lấy danh sách địa chỉ của khách hàng và sắp xếp địa chỉ Default lên đầu tiên.
        /// </summary>
        [Fact]
        public async Task GetByCustomerIdAsync_ShouldOrderByDefaultFirst_ThenCreatedDate()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH01", Name = "Nguyễn Văn A", PhoneNumber = "0901112223" });
            context.CustomerAddresses.AddRange(
                new CustomerAddress { Id = 1, CustomerId = 1, ReceiverName = "Địa chỉ phụ", Phone = "0901112223", Province = "TP.HCM", District = "Q1", Ward = "Bến Nghé", StreetAddress = "1 Lê Duẩn", IsDefault = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new CustomerAddress { Id = 2, CustomerId = 1, ReceiverName = "Địa chỉ chính", Phone = "0901112223", Province = "TP.HCM", District = "Q1", Ward = "Bến Thành", StreetAddress = "10 Đồng Khởi", IsDefault = true, CreatedAt = DateTime.UtcNow.AddDays(-10) }
            );
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            // Act
            var result = (await service.GetByCustomerIdAsync(1)).ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(2); // IsDefault = true lên đầu
            result[0].IsDefault.Should().BeTrue();
            result[1].Id.Should().Be(1);
        }
        #endregion

        #region TC02: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC02: Lấy chi tiết địa chỉ theo ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrNull()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerAddresses.Add(new CustomerAddress
            {
                Id = 1,
                CustomerId = 1,
                ReceiverName = "Trần Thị B",
                Phone = "0988776655",
                Province = "Hà Nội",
                District = "Hoàn Kiếm",
                Ward = "Tràng Tiền",
                StreetAddress = "15 Tràng Tiền",
                IsDefault = true
            });
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            // Act
            var validResult = await service.GetByIdAsync(1);
            var invalidResult = await service.GetByIdAsync(999);

            // Assert
            validResult.Should().NotBeNull();
            validResult!.ReceiverName.Should().Be("Trần Thị B");
            validResult.StreetAddress.Should().Be("15 Tràng Tiền");

            invalidResult.Should().BeNull();
        }
        #endregion

        #region TC03: TẠO ĐỊA CHỈ ĐẦU TIÊN - TỰ ĐỘNG LÀM DEFAULT
        /// <summary>
        /// TC03: Nếu khách hàng chưa có địa chỉ nào, địa chỉ đầu tiên tạo ra BẮT BUỘC có IsDefault = true.
        /// </summary>
        [Fact]
        public async Task CreateAsync_FirstAddress_ShouldBeAutomaticallyMarkedDefault()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH01", Name = "Lê Văn C", PhoneNumber = "0933445566" });
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            var dto = new CustomerAddressCreateDto
            {
                ReceiverName = "Lê Văn C",
                Phone = "0933445566",
                Province = "Đà Nẵng",
                District = "Hải Châu",
                Ward = "Hải Châu 1",
                StreetAddress = "20 Bạch Đằng",
                IsDefault = false // Người dùng không tích chọn nhưng là địa chỉ đầu tiên
            };

            // Act
            var newId = await service.CreateAsync(1, dto);

            // Assert
            var entity = await context.CustomerAddresses.FindAsync(newId);
            entity.Should().NotBeNull();
            entity!.IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC04: TẠO ĐỊA CHỈ MỚI LÀ DEFAULT - TỰ ĐỘNG GỠ DEFAULT ĐỊA CHỈ CŨ
        /// <summary>
        /// TC04: Khi tạo thêm địa chỉ mới có IsDefault = true, địa chỉ mặc định cũ sẽ bị chuyển về false.
        /// </summary>
        [Fact]
        public async Task CreateAsync_SubsequentAddress_WithIsDefaultTrue_ShouldUnmarkPreviousDefault()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH01", Name = "Lê Văn C", PhoneNumber = "0933445566" });
            context.CustomerAddresses.Add(new CustomerAddress
            {
                Id = 10,
                CustomerId = 1,
                ReceiverName = "Địa chỉ cũ",
                Phone = "0933445566",
                Province = "Đà Nẵng",
                District = "Hải Châu",
                Ward = "Hải Châu 1",
                StreetAddress = "100 Hùng Vương",
                IsDefault = true
            });
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            var dto = new CustomerAddressCreateDto
            {
                ReceiverName = "Địa chỉ mới",
                Phone = "0933445566",
                Province = "Đà Nẵng",
                District = "Hải Châu",
                Ward = "Hải Châu 2",
                StreetAddress = "200 Nguyễn Văn Linh",
                IsDefault = true
            };

            // Act
            var newId = await service.CreateAsync(1, dto);

            // Assert
            var oldAddress = await context.CustomerAddresses.FindAsync(10);
            var newAddress = await context.CustomerAddresses.FindAsync(newId);

            oldAddress!.IsDefault.Should().BeFalse();
            newAddress!.IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC05: TẠO ĐỊA CHỈ KHI KHÁCH HÀNG KHÔNG TỒN TẠI
        /// <summary>
        /// TC05: Ném KeyNotFoundException khi tạo địa chỉ cho CustomerId không tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowKeyNotFoundException_WhenCustomerNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new CustomerAddressService(context, _mapper);

            var dto = new CustomerAddressCreateDto
            {
                ReceiverName = "Tester",
                Phone = "0900000000",
                Province = "HCM",
                District = "Q1",
                Ward = "W1",
                StreetAddress = "123 Street"
            };

            // Act & Assert
            var act = () => service.CreateAsync(999, dto);
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy khách hàng.*");
        }
        #endregion

        #region TC06: CẬP NHẬT ĐỊA CHỈ - THIẾT LẬP DEFAULT GỠ DEFAULT KHÁC
        /// <summary>
        /// TC06: Cập nhật địa chỉ thành Default sẽ gỡ cờ Default của các địa chỉ khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_SettingDefault_ShouldUnmarkOtherDefaults()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerAddresses.AddRange(
                new CustomerAddress { Id = 1, CustomerId = 10, ReceiverName = "A1", Phone = "0901", Province = "P", District = "D", Ward = "W", StreetAddress = "S1", IsDefault = true },
                new CustomerAddress { Id = 2, CustomerId = 10, ReceiverName = "A2", Phone = "0902", Province = "P", District = "D", Ward = "W", StreetAddress = "S2", IsDefault = false }
            );
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            var updateDto = new CustomerAddressUpdateDto
            {
                ReceiverName = "A2 Updated",
                Phone = "0902",
                Province = "P",
                District = "D",
                Ward = "W",
                StreetAddress = "S2",
                IsDefault = true
            };

            // Act
            var result = await service.UpdateAsync(2, updateDto);

            // Assert
            result.Should().BeTrue();

            var a1 = await context.CustomerAddresses.FindAsync(1);
            var a2 = await context.CustomerAddresses.FindAsync(2);

            a1!.IsDefault.Should().BeFalse();
            a2!.IsDefault.Should().BeTrue();
            a2.ReceiverName.Should().Be("A2 Updated");
        }
        #endregion

        #region TC07: XÓA ĐỊA CHỈ DEFAULT - TỰ ĐỘNG CHUYỂN QUYỀN DEFAULT CHO ĐỊA CHỈ KHÁC
        /// <summary>
        /// TC07: Xóa địa chỉ đang làm mặc định sẽ tự động luân chuyển quyền mặc định cho địa chỉ còn lại.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_DefaultAddress_ShouldTransferDefaultToNextAddress()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerAddresses.AddRange(
                new CustomerAddress { Id = 1, CustomerId = 10, ReceiverName = "A1", Phone = "0901", Province = "P", District = "D", Ward = "W", StreetAddress = "S1", IsDefault = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new CustomerAddress { Id = 2, CustomerId = 10, ReceiverName = "A2", Phone = "0902", Province = "P", District = "D", Ward = "W", StreetAddress = "S2", IsDefault = false, CreatedAt = DateTime.UtcNow.AddDays(-2) }
            );
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            // Act: Xóa địa chỉ Id = 1 (đang là default)
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            var a2 = await context.CustomerAddresses.FindAsync(2);
            a2!.IsDefault.Should().BeTrue(); // A2 được đôn lên làm default
        }
        #endregion

        #region TC08: THIẾT LẬP MẶC ĐỊNH THÀNH CÔNG (SET DEFAULT)
        /// <summary>
        /// TC08: Gọi SetDefaultAsync đổi địa chỉ mặc định chính xác.
        /// </summary>
        [Fact]
        public async Task SetDefaultAsync_ShouldSwitchDefaultAddressSuccessfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerAddresses.AddRange(
                new CustomerAddress { Id = 1, CustomerId = 5, ReceiverName = "Nhà riêng", Phone = "0901", Province = "P", District = "D", Ward = "W", StreetAddress = "S1", IsDefault = true },
                new CustomerAddress { Id = 2, CustomerId = 5, ReceiverName = "Công ty", Phone = "0902", Province = "P", District = "D", Ward = "W", StreetAddress = "S2", IsDefault = false }
            );
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            // Act
            var result = await service.SetDefaultAsync(2, 5);

            // Assert
            result.Should().BeTrue();

            var a1 = await context.CustomerAddresses.FindAsync(1);
            var a2 = await context.CustomerAddresses.FindAsync(2);

            a1!.IsDefault.Should().BeFalse();
            a2!.IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC09: SET DEFAULT KHI SAI CUSTOMER ID
        /// <summary>
        /// TC09: Ném KeyNotFoundException khi cố tình gán địa chỉ của người khác làm mặc định.
        /// </summary>
        [Fact]
        public async Task SetDefaultAsync_ShouldThrowKeyNotFoundException_WhenCustomerMismatch()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerAddresses.Add(new CustomerAddress { Id = 1, CustomerId = 5, ReceiverName = "Nhà riêng", Phone = "0901", Province = "P", District = "D", Ward = "W", StreetAddress = "S1", IsDefault = true });
            await context.SaveChangesAsync();

            var service = new CustomerAddressService(context, _mapper);

            // Act & Assert
            var act = () => service.SetDefaultAsync(1, 999);
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Địa chỉ không tồn tại hoặc không thuộc quyền sở hữu của khách hàng.*");
        }
        #endregion
    }
}
