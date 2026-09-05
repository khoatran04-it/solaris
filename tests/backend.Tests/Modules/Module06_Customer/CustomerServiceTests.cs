using AutoMapper;
using backend.DTOs.CustomerAddressDTOs;
using backend.DTOs.CustomerDTOs;
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
    /// UNIT TEST: CustomerService (Hồ sơ Khách Hàng, Đa Lọc, Gán Nhóm & Sổ Địa Chỉ)
    /// ============================================================================
    /// </summary>
    public class CustomerServiceTests
    {
        private readonly IMapper _mapper;

        public CustomerServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: LẤY DANH SÁCH TẤT CẢ KÈM CÁC THỰC THỂ LIÊN KẾT
        /// <summary>
        /// TC01: Lấy danh sách toàn bộ khách hàng kèm quan hệ Type, Tier và GroupLinks.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllWithRelationships()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var customerType = new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ" };
            var customerTier = new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng", DiscountPercent = 5 };
            var group = new CustomerGroup { Id = 1, Code = "VIP", Name = "Khách VIP" };

            context.CustomerTypes.Add(customerType);
            context.CustomerTiers.Add(customerTier);
            context.CustomerGroups.Add(group);

            var customer = new Customer
            {
                Id = 1,
                Code = "KH001",
                Name = "Công ty Minh Long",
                PhoneNumber = "0909123456",
                CustomerTypeId = 1,
                CustomerTierId = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.Customers.Add(customer);
            context.CustomerGroupLinks.Add(new CustomerGroupLink { CustomerId = 1, CustomerGroupId = 1, AssignedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act
            var result = (await service.GetAllListAsync()).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Code.Should().Be("KH001");
            result[0].CustomerTypeName.Should().Be("Khách sỉ");
            result[0].CustomerTierName.Should().Be("Hạng Vàng");
            result[0].Groups.Should().HaveCount(1);
            result[0].Groups.First().Should().Be("Khách VIP");
        }
        #endregion

        #region TC02: TÌM KIẾM & BỘ LỌC ĐA TIÊU CHÍ (TYPE, TIER, GROUP, SEARCH)
        /// <summary>
        /// TC02: Lọc khách hàng đa tiêu chí: Phân loại (Type), Bậc hạng (Tier), Nhóm (Group) và từ khóa.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_MultiFilter_CustomerType_CustomerTier_CustomerGroup_Search()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.AddRange(
                new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ" },
                new CustomerType { Id = 2, Code = "LE", Name = "Khách lẻ" }
            );
            context.CustomerTiers.AddRange(
                new CustomerTier { Id = 1, Code = "GOLD", Name = "Vàng" },
                new CustomerTier { Id = 2, Code = "SILVER", Name = "Bạc" }
            );
            context.CustomerGroups.AddRange(
                new CustomerGroup { Id = 10, Code = "VIP", Name = "Nhóm VIP" },
                new CustomerGroup { Id = 20, Code = "PROMO", Name = "Nhóm Khuyến Mãi" }
            );

            context.Customers.AddRange(
                new Customer { Id = 1, Code = "KH01", Name = "Đại Lý 1", PhoneNumber = "0901", CustomerTypeId = 1, CustomerTierId = 1, IsActive = true },
                new Customer { Id = 2, Code = "KH02", Name = "Khách Lẻ 1", PhoneNumber = "0902", CustomerTypeId = 2, CustomerTierId = 2, IsActive = true },
                new Customer { Id = 3, Code = "KH03", Name = "Đại Lý 2", PhoneNumber = "0903", CustomerTypeId = 1, CustomerTierId = 2, IsActive = false }
            );

            context.CustomerGroupLinks.Add(new CustomerGroupLink { CustomerId = 1, CustomerGroupId = 10 });
            context.CustomerGroupLinks.Add(new CustomerGroupLink { CustomerId = 2, CustomerGroupId = 20 });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act 1: Lọc theo CustomerType = "1"
            var typeResult = await service.GetPagedAsync(null, "1", null, null, null, null, null, 1, 10);

            // Act 2: Lọc theo CustomerTier = "2"
            var tierResult = await service.GetPagedAsync(null, null, "2", null, null, null, null, 1, 10);

            // Act 3: Lọc theo CustomerGroup = "10"
            var groupResult = await service.GetPagedAsync(null, null, null, "10", null, null, null, 1, 10);

            // Act 4: Tìm kiếm theo số điện thoại
            var searchResult = await service.GetPagedAsync("0903", null, null, null, null, null, null, 1, 10);

            // Assert
            typeResult.TotalRecords.Should().Be(2); // KH01, KH03
            tierResult.TotalRecords.Should().Be(2); // KH02, KH03
            groupResult.TotalRecords.Should().Be(1); // KH01
            groupResult.Items.First().Code.Should().Be("KH01");
            searchResult.TotalRecords.Should().Be(1); // KH03
            searchResult.Items.First().Code.Should().Be("KH03");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID (ENRICHED DATA)
        /// <summary>
        /// TC03: Lấy chi tiết hồ sơ khách hàng kèm đầy đủ sổ địa chỉ, phân loại, hạng và danh sách GroupIds.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldEnrichType_Tier_Addresses_And_GroupIds()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerTypes.Add(new CustomerType { Id = 1, Code = "SI", Name = "Khách sỉ" });
            context.CustomerTiers.Add(new CustomerTier { Id = 1, Code = "GOLD", Name = "Hạng Vàng", DiscountPercent = 5 });
            context.CustomerGroups.Add(new CustomerGroup { Id = 10, Code = "VIP", Name = "Khách VIP" });

            var customer = new Customer
            {
                Id = 1,
                Code = "KH001",
                Name = "Công ty Thiên Phúc",
                PhoneNumber = "0987654321",
                CustomerTypeId = 1,
                CustomerTierId = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.Customers.Add(customer);
            context.CustomerAddresses.Add(new CustomerAddress { Id = 1, CustomerId = 1, ReceiverName = "VP Chính", Phone = "0987654321", Province = "HCM", District = "Q1", Ward = "Bến Nghé", StreetAddress = "1 Hai Bà Trưng", IsDefault = true });
            context.CustomerGroupLinks.Add(new CustomerGroupLink { CustomerId = 1, CustomerGroupId = 10 });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Code.Should().Be("KH001");
            result.CustomerTypeName.Should().Be("Khách sỉ");
            result.CustomerTierName.Should().Be("Hạng Vàng");
            result.Addresses.Should().HaveCount(1);
            result.Addresses.First().StreetAddress.Should().Be("1 Hai Bà Trưng");
            result.GroupIds.Should().Contain(10);
        }
        #endregion

        #region TC04: TẠO MỚI HỒ SƠ PHỨC HỢP (KÈM NHÓM & SỔ ĐỊA CHỈ TRONG 1 TRANSACTION)
        /// <summary>
        /// TC04: Tạo mới khách hàng kèm GroupIds và danh sách địa chỉ ban đầu trong cùng 1 Transaction.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ComplexTransaction_ShouldCreateCustomer_GroupLinks_And_InitialAddresses()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.AddRange(
                new CustomerGroup { Id = 1, Code = "G1", Name = "Nhóm 1" },
                new CustomerGroup { Id = 2, Code = "G2", Name = "Nhóm 2" }
            );
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            var createDto = new CustomerCreateDto
            {
                Code = "  kh-new-01  ",
                Name = "  Công ty Hoàng Nam  ",
                PhoneNumber = "  0911223344  ",
                Email = "  hoangnam@gmail.com  ",
                TaxCode = "  0312345678  ",
                IsActive = true,
                GroupIds = new List<int> { 1, 2 },
                Addresses = new List<CustomerAddressCreateDto>
                {
                    new CustomerAddressCreateDto
                    {
                        ReceiverName = "Kho 1",
                        Phone = "0911223344",
                        Province = "Bình Dương",
                        District = "Thuận An",
                        Ward = "An Phú",
                        StreetAddress = "KCN VSIP 1",
                        IsDefault = true
                    },
                    new CustomerAddressCreateDto
                    {
                        ReceiverName = "Kho 2",
                        Phone = "0911223344",
                        Province = "Đồng Nai",
                        District = "Biên Hòa",
                        Ward = "Long Bình",
                        StreetAddress = "KCN Amata",
                        IsDefault = false
                    }
                }
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var savedCustomer = await context.Customers
                .Include(c => c.Addresses)
                .Include(c => c.GroupLinks)
                .FirstOrDefaultAsync(c => c.Id == newId);

            savedCustomer.Should().NotBeNull();
            savedCustomer!.Code.Should().Be("KH-NEW-01");
            savedCustomer.Name.Should().Be("Công ty Hoàng Nam");
            savedCustomer.PhoneNumber.Should().Be("0911223344");
            savedCustomer.GroupLinks.Should().HaveCount(2);
            savedCustomer.Addresses.Should().HaveCount(2);

            var defaultAddr = savedCustomer.Addresses.First(a => a.IsDefault);
            defaultAddr.StreetAddress.Should().Be("KCN VSIP 1");
        }
        #endregion

        #region TC05: CHẶN TRÙNG MÃ KHÁCH HÀNG HOẶC SỐ ĐIỆN THOẠI
        /// <summary>
        /// TC05: Ném InvalidOperationException khi tạo khách hàng trùng mã Code hoặc số điện thoại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCodeOrPhone()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Khách cũ", PhoneNumber = "0909999888" });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            var dupCodeDto = new CustomerCreateDto { Code = "kh001", Name = "Khách mới", PhoneNumber = "0900000000" };
            var dupPhoneDto = new CustomerCreateDto { Code = "KH002", Name = "Khách mới 2", PhoneNumber = "0909999888" };

            // Act & Assert 1: Trùng mã
            var actCode = () => service.CreateAsync(dupCodeDto);
            await actCode.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã khách hàng 'kh001' đã tồn tại.*");

            // Act & Assert 2: Trùng số điện thoại
            var actPhone = () => service.CreateAsync(dupPhoneDto);
            await actPhone.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Số điện thoại '0909999888' đã được đăng ký cho khách hàng khác.*");
        }
        #endregion

        #region TC06: CẬP NHẬT HỒ SƠ & THAY THẾ DANH SÁCH GROUPLINKS
        /// <summary>
        /// TC06: Cập nhật hồ sơ và thay thế danh sách GroupIds (xóa liên kết cũ, tạo liên kết mới).
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateCustomerInfo_And_ReplaceGroupLinks()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CustomerGroups.AddRange(
                new CustomerGroup { Id = 10, Code = "G10", Name = "Nhóm 10" },
                new CustomerGroup { Id = 20, Code = "G20", Name = "Nhóm 20" },
                new CustomerGroup { Id = 30, Code = "G30", Name = "Nhóm 30" }
            );
            var customer = new Customer { Id = 1, Code = "KH001", Name = "Công ty A", PhoneNumber = "0901234567" };
            context.Customers.Add(customer);
            context.CustomerGroupLinks.Add(new CustomerGroupLink { CustomerId = 1, CustomerGroupId = 10 });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            var updateDto = new CustomerUpdateDto
            {
                Code = "KH001_UPDATED",
                Name = "Công ty A Toàn Cầu",
                PhoneNumber = "0901234567",
                Email = "info@congtya.com",
                IsActive = true,
                GroupIds = new List<int> { 20, 30 } // Đổi nhóm sang 20 và 30
            };

            // Act
            var result = await service.UpdateAsync(1, updateDto);

            // Assert
            result.Should().BeTrue();

            var updatedCustomer = await context.Customers
                .Include(c => c.GroupLinks)
                .FirstOrDefaultAsync(c => c.Id == 1);

            updatedCustomer!.Code.Should().Be("KH001_UPDATED");
            updatedCustomer.Name.Should().Be("Công ty A Toàn Cầu");
            var activeGroupLinks = updatedCustomer!.GroupLinks.Where(g => !g.IsDeleted).ToList();
            activeGroupLinks.Should().HaveCount(2);
            activeGroupLinks.Select(g => g.CustomerGroupId).Should().BeEquivalentTo(new[] { 20, 30 });

            var readDto = await service.GetByIdAsync(1);
            readDto!.GroupIds.Should().BeEquivalentTo(new[] { 20, 30 });
        }
        #endregion

        #region TC07: CẬP NHẬT CHẶN TRÙNG MÃ HOẶC SỐ ĐIỆN THOẠI VỚI KHÁCH HÀNG KHÁC
        /// <summary>
        /// TC07: Ném InvalidOperationException khi cập nhật trùng mã hoặc số điện thoại với bản ghi khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCodeOrPhoneWithOtherCustomer()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.AddRange(
                new Customer { Id = 1, Code = "KH001", Name = "Khách 1", PhoneNumber = "0901" },
                new Customer { Id = 2, Code = "KH002", Name = "Khách 2", PhoneNumber = "0902" }
            );
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            var dupCodeDto = new CustomerUpdateDto { Code = "KH002", Name = "Khách 1", PhoneNumber = "0901" };
            var dupPhoneDto = new CustomerUpdateDto { Code = "KH001", Name = "Khách 1", PhoneNumber = "0902" };

            // Act & Assert 1: Trùng mã với ID 2
            var actCode = () => service.UpdateAsync(1, dupCodeDto);
            await actCode.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã khách hàng 'KH002' đã tồn tại.*");

            // Act & Assert 2: Trùng phone với ID 2
            var actPhone = () => service.UpdateAsync(1, dupPhoneDto);
            await actPhone.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Số điện thoại '0902' đã được đăng ký bởi khách hàng khác.*");
        }
        #endregion

        #region TC08: SAFETY SHIELD - CHẶN XÓA KHI CÓ ĐƠN HÀNG (ORDERS)
        /// <summary>
        /// TC08: Chặn xóa khách hàng nếu khách hàng đã có lịch sử Đơn Hàng (Orders).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_SafetyShield_ShouldThrowInvalidOperationException_WhenCustomerHasOrders()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Khách A", PhoneNumber = "0901" });
            context.Orders.Add(new Order
            {
                Id = 100,
                OrderCode = "ORD-2026-001",
                CustomerId = 1,
                TotalAmount = 500000,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa khách hàng này vì đã có lịch sử Đơn Hàng trong hệ thống.*");
        }
        #endregion

        #region TC09: SAFETY SHIELD - CHẶN XÓA KHI CÓ PHIẾU TRẢ HÀNG (RETURNS)
        /// <summary>
        /// TC09: Chặn xóa khách hàng nếu khách hàng đã có lịch sử Phiếu Trả Hàng (CustomerReturns).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_SafetyShield_ShouldThrowInvalidOperationException_WhenCustomerHasReturns()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Khách A", PhoneNumber = "0901" });
            context.CustomerReturns.Add(new CustomerReturn
            {
                Id = 200,
                ReturnCode = "RET-2026-001",
                CustomerId = 1,
                RefundAmount = 100000,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa khách hàng này vì đã có lịch sử Phiếu Trả Hàng trong hệ thống.*");
        }
        #endregion

        #region TC10: XÓA THÀNH CÔNG KHI KHÔNG CÓ RÀNG BUỘC TÀI CHÍNH
        /// <summary>
        /// TC10: Xóa thành công khách hàng độc lập chưa phát sinh giao dịch.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldDeleteSuccessfully_WhenNoFinancialConstraints()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH_TEMP", Name = "Khách Thử Nghiệm", PhoneNumber = "0999000111" });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedCustomer = await context.Customers.FindAsync(1);
            deletedCustomer!.IsDeleted.Should().BeTrue();
            deletedCustomer.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC11: CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC11: Khóa / Mở khóa tài khoản khách hàng thành công.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleStatusCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Customers.Add(new Customer { Id = 1, Code = "KH001", Name = "Khách A", PhoneNumber = "0901", IsActive = true });
            await context.SaveChangesAsync();

            var service = new CustomerService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.Customers.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
