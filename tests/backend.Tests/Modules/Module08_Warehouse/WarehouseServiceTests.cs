using AutoMapper;
using backend.DTOs.InventoryDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module08_Warehouse
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 08: WAREHOUSE & PHYSICAL ADDRESS MANAGEMENT
    /// 🧪 TEST SUITE: WarehouseServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Quản lý Kho Hàng (Warehouse):
    /// - Quản lý hồ sơ kho vật lý và tọa độ địa lý GPS (WarehouseAddress)
    /// - Trích xuất Enriched Data & Flatten địa chỉ hành chính
    /// - Tự động chuẩn hóa mã kho in hoa (Uppercase Code)
    /// - Kiểm soát quan hệ quản lý kho (Manager / IAUser)
    /// - Thực thi 8 Tầng Khiên An Toàn Tồn Kho & Chứng Từ (Safety Shields)
    /// - Đồng bộ chu trình sống Soft Delete (Xóa mềm đồng thời Kho và Địa chỉ)
    /// - Đảm bảo bọc toàn bộ mutations trong Database Transaction
    /// </summary>
    public class WarehouseServiceTests
    {
        private readonly IMapper _mapper;

        public WarehouseServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        /// <summary>
        /// Chuẩn bị dữ liệu mẫu cho Trưởng kho (IAUser) để phục vụ kiểm thử liên kết nhân sự.
        /// </summary>
        private static async Task SeedSampleUsersAsync(Data.SolarisDbContext context)
        {
            var manager = new IAUser
            {
                Id = 1,
                CitizenId = "001200001234",
                Username = "truongkhohn",
                FullName = "Nguyễn Văn Trưởng Kho",
                Email = "truongkho@solaris.vn",
                PhoneNumber = "0987654321",
                PasswordHash = "hashed_password",
                IsActive = true,
                IsDeleted = false
            };
            context.IAUsers.Add(manager);
            await context.SaveChangesAsync();
        }
        #endregion

        // ============================================================================
        // 1. TEST CASES: GET ALL & GET PAGED (QUERIES)
        // ============================================================================
        #region Query Tests

        /// <summary>
        /// TC01: Lấy toàn bộ danh sách kho hàng, sắp xếp theo tên và làm phẳng (Flatten) địa chỉ + tên trưởng kho.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllWarehouses_WithAddressAndManager_RespectActiveFilter()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleUsersAsync(context);

            var address1 = new WarehouseAddress
            {
                Id = 1,
                Province = "Hà Nội",
                District = "Long Biên",
                Ward = "Gia Thụy",
                StreetAddress = "Số 123 Đường Nguyễn Sơn",
                Latitude = 21.0456,
                Longitude = 105.8821
            };

            var address2 = new WarehouseAddress
            {
                Id = 2,
                Province = "Lâm Đồng",
                District = "Đà Lạt",
                Ward = "Phường 8",
                StreetAddress = "Vườn Nông Sản Solaris",
                Latitude = 11.9404,
                Longitude = 108.4583
            };
            context.WarehouseAddresses.AddRange(address1, address2);

            var warehouse1 = new Warehouse
            {
                Id = 1,
                Code = "WH-HN-01",
                Name = "Tổng Kho Hà Nội",
                WarehouseType = "Mega Hub",
                AddressId = 1,
                ManagerId = 1,
                IsActive = true
            };

            var warehouse2 = new Warehouse
            {
                Id = 2,
                Code = "WH-DL-01",
                Name = "Kho Nông Sản Đà Lạt",
                WarehouseType = "Sorting Center",
                AddressId = 2,
                ManagerId = null,
                IsActive = false
            };
            context.Warehouses.AddRange(warehouse1, warehouse2);
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            // Act 1: Lấy tất cả
            var allResult = (await service.GetAllListAsync(isActiveOnly: false)).ToList();

            // Act 2: Chỉ lấy kho Active
            var activeResult = (await service.GetAllListAsync(isActiveOnly: true)).ToList();

            // Assert
            allResult.Should().HaveCount(2);
            // Sắp xếp theo Name tăng dần: "Kho Nông Sản Đà Lạt" trước "Tổng Kho Hà Nội"
            allResult[0].Code.Should().Be("WH-DL-01");
            allResult[1].Code.Should().Be("WH-HN-01");

            // Kiểm tra Flatten địa chỉ và ManagerName của kho Hà Nội
            var hnDto = allResult[1];
            hnDto.ManagerName.Should().Be("Nguyễn Văn Trưởng Kho");
            hnDto.Province.Should().Be("Hà Nội");
            hnDto.District.Should().Be("Long Biên");
            hnDto.Ward.Should().Be("Gia Thụy");
            hnDto.StreetAddress.Should().Be("Số 123 Đường Nguyễn Sơn");
            hnDto.FullAddress.Should().Be("Số 123 Đường Nguyễn Sơn, Gia Thụy, Long Biên, Hà Nội");
            hnDto.Latitude.Should().Be(21.0456);
            hnDto.Longitude.Should().Be(105.8821);

            activeResult.Should().HaveCount(1);
            activeResult[0].Code.Should().Be("WH-HN-01");
        }

        /// <summary>
        /// TC02: Phân trang danh sách kho hàng, tìm kiếm theo Mã/Tên và lọc theo Tỉnh thành.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterBySearch_ActiveStatus_And_Province()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var addr1 = new WarehouseAddress { Id = 1, Province = "TP Hồ Chí Minh", District = "Quận 7", Ward = "Tân Phong", StreetAddress = "Nguyễn Văn Linh" };
            var addr2 = new WarehouseAddress { Id = 2, Province = "TP Hồ Chí Minh", District = "Thủ Đức", Ward = "Linh Trung", StreetAddress = "Kha Vạn Cân" };
            var addr3 = new WarehouseAddress { Id = 3, Province = "Đà Nẵng", District = "Hải Châu", Ward = "Hải Châu 1", StreetAddress = "Bạch Đằng" };
            context.WarehouseAddresses.AddRange(addr1, addr2, addr3);

            context.Warehouses.AddRange(
                new Warehouse { Id = 1, Code = "WH-HCM-01", Name = "Kho Nam Sài Gòn", AddressId = 1, IsActive = true, CreatedAt = now.AddDays(-2) },
                new Warehouse { Id = 2, Code = "WH-HCM-02", Name = "Kho Đông Sài Gòn", AddressId = 2, IsActive = true, CreatedAt = now.AddDays(-1) },
                new Warehouse { Id = 3, Code = "WH-DN-01", Name = "Kho Trung Bộ Đà Nẵng", AddressId = 3, IsActive = false, CreatedAt = now }
            );
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "Sài Gòn"
            var searchResult = await service.GetPagedAsync("Sài Gòn", null, null, 1, 10);

            // Act 2: Lọc theo Tỉnh thành "TP Hồ Chí Minh" và IsActive = true
            var filterResult = await service.GetPagedAsync(null, true, "TP Hồ Chí Minh", 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(2);
            searchResult.Items.Select(x => x.Code).Should().Contain(new[] { "WH-HCM-01", "WH-HCM-02" });

            filterResult.TotalRecords.Should().Be(2);
            filterResult.Items.Select(x => x.Province).All(p => p == "TP Hồ Chí Minh").Should().BeTrue();
        }

        /// <summary>
        /// TC03: Lấy chi tiết kho hàng theo ID và trả về null nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnEnrichedWarehouse_WithAddressAndManager()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleUsersAsync(context);

            var address = new WarehouseAddress
            {
                Id = 1,
                Province = "Cần Thơ",
                District = "Ninh Kiều",
                Ward = "An Khánh",
                StreetAddress = "Đường 3 Tháng 2",
                Latitude = 10.0333,
                Longitude = 105.7833
            };
            context.WarehouseAddresses.Add(address);

            var warehouse = new Warehouse
            {
                Id = 1,
                Code = "WH-CT-01",
                Name = "Kho Nông Sản Tây Đô",
                WarehouseType = "Retail Store",
                AddressId = 1,
                ManagerId = 1,
                IsActive = true
            };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(1);
            var notFoundResult = await service.GetByIdAsync(999);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Code.Should().Be("WH-CT-01");
            result.Name.Should().Be("Kho Nông Sản Tây Đô");
            result.ManagerName.Should().Be("Nguyễn Văn Trưởng Kho");
            result.Province.Should().Be("Cần Thơ");
            result.FullAddress.Should().Be("Đường 3 Tháng 2, An Khánh, Ninh Kiều, Cần Thơ");

            notFoundResult.Should().BeNull();
        }

        #endregion

        // ============================================================================
        // 2. TEST CASES: CREATE & UPDATE (COMMANDS)
        // ============================================================================
        #region Command Tests

        /// <summary>
        /// TC04: Tạo mới kho hàng và địa chỉ đồng thời trong 1 Transaction, tự động viết hoa mã kho.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateWarehouse_And_WarehouseAddress_InTransaction()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleUsersAsync(context);

            var service = new WarehouseService(context, _mapper);

            var createDto = new WarehouseCreateDto
            {
                Code = "wh-bd-01", // Chữ thường để kiểm tra tự động in hoa
                Name = "Tổng Kho Bình Dương",
                WarehouseType = "Mega Hub",
                ManagerId = 1,
                IsActive = true,
                Address = new WarehouseAddressPayload
                {
                    Province = "Bình Dương",
                    District = "Thủ Dầu Một",
                    Ward = "Phú Hòa",
                    StreetAddress = "Đại Lộ Bình Dương",
                    Latitude = 10.9805,
                    Longitude = 106.6748
                }
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var savedWarehouse = await context.Warehouses
                .Include(w => w.Address)
                .FirstOrDefaultAsync(w => w.Id == newId);

            savedWarehouse.Should().NotBeNull();
            savedWarehouse!.Code.Should().Be("WH-BD-01"); // Chuẩn hóa in hoa
            savedWarehouse.Name.Should().Be("Tổng Kho Bình Dương");
            savedWarehouse.ManagerId.Should().Be(1);
            savedWarehouse.Address.Should().NotBeNull();
            savedWarehouse.Address!.Province.Should().Be("Bình Dương");
            savedWarehouse.Address.FullAddress.Should().Be("Đại Lộ Bình Dương, Phú Hòa, Thủ Dầu Một, Bình Dương");
        }

        /// <summary>
        /// TC05: Kiểm tra các lỗi nghiệp vụ khi tạo mới (Trống mã/tên, thiếu địa chỉ, trùng mã/tên, sai ManagerId).
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenValidationFails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var existingAddress = new WarehouseAddress { Id = 1, Province = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng", StreetAddress = "Xuân Thủy" };
            context.WarehouseAddresses.Add(existingAddress);
            context.Warehouses.Add(new Warehouse { Id = 1, Code = "WH-HN-01", Name = "Tổng Kho Hà Nội", AddressId = 1 });
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            var validAddress = new WarehouseAddressPayload
            {
                Province = "Hà Nội",
                District = "Hoàng Mai",
                Ward = "Giáp Bát",
                StreetAddress = "Giải Phóng"
            };

            // 1. Mã kho rỗng
            var emptyCodeDto = new WarehouseCreateDto { Code = "  ", Name = "Kho Mới", Address = validAddress };
            var actEmptyCode = async () => await service.CreateAsync(emptyCodeDto);
            await actEmptyCode.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã kho không được để trống*");

            // 2. Tên kho rỗng
            var emptyNameDto = new WarehouseCreateDto { Code = "WH-NEW-01", Name = "   ", Address = validAddress };
            var actEmptyName = async () => await service.CreateAsync(emptyNameDto);
            await actEmptyName.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tên kho không được để trống*");

            // 3. Thiếu thông tin địa chỉ bắt buộc
            var invalidAddrDto = new WarehouseCreateDto
            {
                Code = "WH-NEW-01",
                Name = "Kho Mới",
                Address = new WarehouseAddressPayload { Province = "", District = "Quận 1", Ward = "", StreetAddress = "" }
            };
            var actInvalidAddr = async () => await service.CreateAsync(invalidAddrDto);
            await actInvalidAddr.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Thông tin địa chỉ kho không hợp lệ*");

            // 4. Trùng Mã kho (case-insensitive)
            var duplicateCodeDto = new WarehouseCreateDto { Code = "wh-hn-01", Name = "Kho Hà Nội 2", Address = validAddress };
            var actDuplicateCode = async () => await service.CreateAsync(duplicateCodeDto);
            await actDuplicateCode.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã kho 'wh-hn-01' đã tồn tại*");

            // 5. Trùng Tên kho (case-insensitive)
            var duplicateNameDto = new WarehouseCreateDto { Code = "WH-HN-02", Name = "tổng kho hà nội", Address = validAddress };
            var actDuplicateName = async () => await service.CreateAsync(duplicateNameDto);
            await actDuplicateName.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tên kho 'tổng kho hà nội' đã tồn tại*");

            // 6. Trưởng kho không tồn tại
            var invalidManagerDto = new WarehouseCreateDto
            {
                Code = "WH-HN-03",
                Name = "Kho Mới 3",
                ManagerId = 999,
                Address = validAddress
            };
            var actInvalidManager = async () => await service.CreateAsync(invalidManagerDto);
            await actInvalidManager.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Trưởng kho được chọn không tồn tại*");
        }

        /// <summary>
        /// TC06: Cập nhật thông tin kho hàng và ghi đè thông tin địa chỉ vật lý cũ.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateWarehouse_And_OverwriteAddress_Successfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleUsersAsync(context);

            var address = new WarehouseAddress
            {
                Id = 1,
                Province = "Hải Phòng",
                District = "Hồng Bàng",
                Ward = "Minh Khai",
                StreetAddress = "Điện Biên Phủ",
                Latitude = 20.8651,
                Longitude = 106.6838
            };
            context.WarehouseAddresses.Add(address);

            var warehouse = new Warehouse
            {
                Id = 1,
                Code = "WH-HP-01",
                Name = "Kho Cảng Hải Phòng",
                WarehouseType = "Mega Hub",
                AddressId = 1,
                ManagerId = null,
                IsActive = true
            };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            var updateDto = new WarehouseUpdateDto
            {
                Name = "Tổng Kho Cảng Hải Phòng (Mở Rộng)",
                WarehouseType = "Sorting Center",
                ManagerId = 1,
                IsActive = true,
                Address = new WarehouseAddressPayload
                {
                    Province = "Hải Phòng",
                    District = "Hải An",
                    Ward = "Đông Hải 2",
                    StreetAddress = "KCN Đình Vũ",
                    Latitude = 20.8350,
                    Longitude = 106.7450
                }
            };

            // Act
            var result = await service.UpdateAsync(1, updateDto);

            // Assert
            result.Should().BeTrue();

            var updatedWarehouse = await context.Warehouses
                .Include(w => w.Address)
                .FirstOrDefaultAsync(w => w.Id == 1);

            updatedWarehouse.Should().NotBeNull();
            updatedWarehouse!.Name.Should().Be("Tổng Kho Cảng Hải Phòng (Mở Rộng)");
            updatedWarehouse.WarehouseType.Should().Be("Sorting Center");
            updatedWarehouse.ManagerId.Should().Be(1);

            // AddressId được giữ nguyên, nội dung bản ghi được ghi đè
            updatedWarehouse.AddressId.Should().Be(1);
            updatedWarehouse.Address.Should().NotBeNull();
            updatedWarehouse.Address!.District.Should().Be("Hải An");
            updatedWarehouse.Address.StreetAddress.Should().Be("KCN Đình Vũ");
            updatedWarehouse.Address.FullAddress.Should().Be("KCN Đình Vũ, Đông Hải 2, Hải An, Hải Phòng");
            updatedWarehouse.Address.Latitude.Should().Be(20.8350);
        }

        /// <summary>
        /// TC07: Ném KeyNotFoundException khi ID không tồn tại hoặc InvalidOperationException khi trùng tên với kho khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldThrowExceptions_OnNotFound_Or_DuplicateName_Or_InvalidManager()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var addr1 = new WarehouseAddress { Id = 1, Province = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng", StreetAddress = "Dịch Vọng" };
            var addr2 = new WarehouseAddress { Id = 2, Province = "Hà Nội", District = "Đống Đa", Ward = "Láng Hạ", StreetAddress = "Láng Hạ" };
            context.WarehouseAddresses.AddRange(addr1, addr2);

            context.Warehouses.AddRange(
                new Warehouse { Id = 1, Code = "WH-01", Name = "Kho Một", AddressId = 1 },
                new Warehouse { Id = 2, Code = "WH-02", Name = "Kho Hai", AddressId = 2 }
            );
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            var validAddress = new WarehouseAddressPayload { Province = "Hà Nội", District = "Ba Đình", Ward = "Kim Mã", StreetAddress = "Kim Mã" };

            // 1. Không tìm thấy ID
            var notFoundDto = new WarehouseUpdateDto { Name = "Tên Bất Kỳ", Address = validAddress };
            var actNotFound = async () => await service.UpdateAsync(999, notFoundDto);
            await actNotFound.Should().ThrowAsync<KeyNotFoundException>();

            // 2. Trùng tên với Kho Hai khi update Kho Một
            var duplicateNameDto = new WarehouseUpdateDto { Name = "kho hai", Address = validAddress };
            var actDuplicate = async () => await service.UpdateAsync(1, duplicateNameDto);
            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã bị trùng*");

            // 3. Trưởng kho không tồn tại
            var invalidManagerDto = new WarehouseUpdateDto { Name = "Kho Một Sửa", ManagerId = 999, Address = validAddress };
            var actInvalidManager = async () => await service.UpdateAsync(1, invalidManagerDto);
            await actInvalidManager.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Trưởng kho được chọn không tồn tại*");
        }

        #endregion

        // ============================================================================
        // 3. TEST CASES: DELETE & TOGGLE ACTIVE
        // ============================================================================
        #region Delete & Toggle Tests

        /// <summary>
        /// TC08: Xóa mềm kho hàng và địa chỉ vật lý khi không có ràng buộc chứng từ hay tồn kho.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldSoftDeleteWarehouse_And_WarehouseAddress()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var address = new WarehouseAddress
            {
                Id = 1,
                Province = "Hà Nội",
                District = "Hà Đông",
                Ward = "Quang Trung",
                StreetAddress = "Quang Trung"
            };
            context.WarehouseAddresses.Add(address);

            var warehouse = new Warehouse
            {
                Id = 1,
                Code = "WH-HD-01",
                Name = "Kho Hà Đông",
                AddressId = 1
            };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            var deletedWarehouse = await context.Warehouses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(w => w.Id == 1);
            deletedWarehouse.Should().NotBeNull();
            deletedWarehouse!.IsDeleted.Should().BeTrue();
            deletedWarehouse.DeletedAt.Should().NotBeNull();

            var deletedAddress = await context.WarehouseAddresses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == 1);
            deletedAddress.Should().NotBeNull();
            deletedAddress!.IsDeleted.Should().BeTrue();
            deletedAddress.DeletedAt.Should().NotBeNull();
        }

        /// <summary>
        /// TC09: Kiểm tra 8 Tầng Khiên An Toàn (Safety Shields) chặn xóa kho khi có tồn kho hoặc chứng từ phát sinh.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldTriggerSafetyShields_WhenReferenced()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var address = new WarehouseAddress { Id = 1, Province = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng", StreetAddress = "Dịch Vọng" };
            context.WarehouseAddresses.Add(address);

            var warehouse = new Warehouse { Id = 1, Code = "WH-TEST", Name = "Kho Kiểm Tra", AddressId = 1 };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            // 1. Khiên Tồn Kho > 0
            var inventory = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 101, BatchId = 1, QuantityAvailable = 50 };
            context.WarehouseInventories.Add(inventory);
            await context.SaveChangesAsync();

            var act1 = async () => await service.DeleteAsync(1);
            await act1.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*vẫn còn tồn kho sản phẩm*");
            context.WarehouseInventories.Remove(inventory);
            await context.SaveChangesAsync();

            // 2. Khiên Phiếu Nhập Kho (InventoryReceipts)
            var receipt = new InventoryReceipt { Id = 1, ReceiptCode = "IR-001", WarehouseId = 1, Status = InventoryReceiptStatus.Completed };
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var act2 = async () => await service.DeleteAsync(1);
            await act2.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phiếu nhập kho*");
            context.InventoryReceipts.Remove(receipt);
            await context.SaveChangesAsync();

            // 3. Khiên Phiếu Xuất Kho (InventoryIssues)
            var issue = new InventoryIssue { Id = 1, IssueCode = "II-001", WarehouseId = 1 };
            context.InventoryIssues.Add(issue);
            await context.SaveChangesAsync();

            var act3 = async () => await service.DeleteAsync(1);
            await act3.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phiếu xuất kho*");
            context.InventoryIssues.Remove(issue);
            await context.SaveChangesAsync();

            // 4. Khiên Phiếu Chuyển Kho (InventoryTransfers)
            var transfer = new InventoryTransfer { Id = 1, TransferCode = "IT-001", FromWarehouseId = 1, ToWarehouseId = 2 };
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var act4 = async () => await service.DeleteAsync(1);
            await act4.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phiếu chuyển kho*");
            context.InventoryTransfers.Remove(transfer);
            await context.SaveChangesAsync();

            // 5. Khiên Phiếu Kiểm Kê (InventoryAudits)
            var audit = new InventoryAudit { Id = 1, AuditCode = "IA-001", WarehouseId = 1 };
            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var act5 = async () => await service.DeleteAsync(1);
            await act5.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phiếu kiểm kê*");
            context.InventoryAudits.Remove(audit);
            await context.SaveChangesAsync();

            // 6. Khiên Phiếu Điều Chỉnh (InventoryAdjustments)
            var adjustment = new InventoryAdjustment { Id = 1, AdjustmentCode = "ADJ-001", WarehouseId = 1 };
            context.InventoryAdjustments.Add(adjustment);
            await context.SaveChangesAsync();

            var act6 = async () => await service.DeleteAsync(1);
            await act6.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phiếu điều chỉnh tồn kho*");
            context.InventoryAdjustments.Remove(adjustment);
            await context.SaveChangesAsync();

            // 7. Khiên Đơn Hàng (Orders)
            var order = new Order { Id = 1, OrderCode = "SO-001", WarehouseId = 1, CustomerId = 1, TotalAmount = 500000 };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var act7 = async () => await service.DeleteAsync(1);
            await act7.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đơn hàng bán xuất từ kho này*");
            context.Orders.Remove(order);
            await context.SaveChangesAsync();

            // 8. Khiên Đơn Trả Hàng (CustomerReturns)
            var returnOrder = new CustomerReturn { Id = 1, ReturnCode = "RET-001", WarehouseId = 1, CustomerId = 1, RefundAmount = 100000 };
            context.CustomerReturns.Add(returnOrder);
            await context.SaveChangesAsync();

            var act8 = async () => await service.DeleteAsync(1);
            await act8.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đơn trả hàng nhập vào kho này*");
        }

        /// <summary>
        /// TC10: Bật / Tắt trạng thái hoạt động ToggleActiveAsync thành công.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleActiveStatus_And_ThrowIfNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var address = new WarehouseAddress { Id = 1, Province = "Hà Nội", District = "Cầu Giấy", Ward = "Dịch Vọng", StreetAddress = "Dịch Vọng" };
            context.WarehouseAddresses.Add(address);

            var warehouse = new Warehouse { Id = 1, Code = "WH-HN-01", Name = "Kho Hà Nội", AddressId = 1, IsActive = true };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = new WarehouseService(context, _mapper);

            // Act 1: Tắt active (true -> false)
            var result1 = await service.ToggleActiveAsync(1);
            var status1 = (await context.Warehouses.FindAsync(1))!.IsActive;

            // Act 2: Bật lại active (false -> true)
            var result2 = await service.ToggleActiveAsync(1);
            var status2 = (await context.Warehouses.FindAsync(1))!.IsActive;

            // Assert
            result1.Should().BeTrue();
            status1.Should().BeFalse();

            result2.Should().BeTrue();
            status2.Should().BeTrue();

            var actNotFound = async () => await service.ToggleActiveAsync(999);
            await actNotFound.Should().ThrowAsync<KeyNotFoundException>();
        }

        #endregion
    }
}
