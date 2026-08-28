using AutoMapper;
using backend.DTOs.SupplierAddressDTOs;
using backend.DTOs.SupplierDTOs;
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
    /// 🧪 UNIT TEST: SupplierService (Quản lý Hồ sơ Nhà cung cấp)
    /// ============================================================================
    /// </summary>
    public class SupplierServiceTests
    {
        private readonly IMapper _mapper;

        public SupplierServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM ĐA CỘT (CODE, NAME, PHONE)
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm đa cột theo Mã định danh, Tên nhà cung cấp hoặc Số điện thoại.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldSearchAcrossMultipleColumns()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.AddRange(
                new Supplier { Id = 1, Code = "NCC01", Name = "Nông trại Xanh Đà Lạt", Phone = "0912345678", Email = "dalat@gmail.com", IsActive = true },
                new Supplier { Id = 2, Code = "NCC02", Name = "Hợp tác xã Bến Tre", Phone = "0987654321", Email = "bentre@gmail.com", IsActive = true },
                new Supplier { Id = 3, Code = "NCC03", Name = "Vựa Trái Cây Long An", Phone = "0909999888", Email = "longan@gmail.com", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act 1: Tìm theo tên "Đà Lạt"
            var nameResult = await service.GetPagedAsync("Đà Lạt", null, null, null, null, 1, 10);

            // Act 2: Tìm theo số điện thoại "999888"
            var phoneResult = await service.GetPagedAsync("999888", null, null, null, null, 1, 10);

            // Act 3: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Assert
            nameResult.TotalRecords.Should().Be(1);
            nameResult.Items.First().Code.Should().Be("NCC01");

            phoneResult.TotalRecords.Should().Be(1);
            phoneResult.Items.First().Code.Should().Be("NCC03");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("NCC03");
        }
        #endregion

        #region TC02: ĐẶC THÙ - LỌC THEO DANH SÁCH PHÂN LOẠI (MULTI-TYPE FILTERING)
        /// <summary>
        /// TC02: Kiểm tra lọc theo chuỗi ID phân loại phân cách dấu phẩy (VD: "1,2"), lọc đơn "1", và xử lý chuỗi rác an toàn.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterBySupplierTypeIds_Correctly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierTypes.AddRange(
                new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn" },
                new SupplierType { Id = 2, Code = "COOP", Name = "Hợp tác xã" },
                new SupplierType { Id = 3, Code = "IMPORT", Name = "Công ty Nhập khẩu" }
            );

            context.Suppliers.AddRange(
                new Supplier { Id = 1, Code = "NCC01", Name = "Vườn A", Phone = "0901", Email = "a@gmail.com", SupplierTypeId = 1 },
                new Supplier { Id = 2, Code = "NCC02", Name = "HTX B", Phone = "0902", Email = "b@gmail.com", SupplierTypeId = 2 },
                new Supplier { Id = 3, Code = "NCC03", Name = "Công ty C", Phone = "0903", Email = "c@gmail.com", SupplierTypeId = 3 }
            );
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act 1: Lọc nhiều loại "1,2" (Nhà vườn + Hợp tác xã)
            var multiResult = await service.GetPagedAsync(null, "1,2", null, null, null, 1, 10);

            // Act 2: Lọc 1 loại "3" (Công ty Nhập khẩu)
            var singleResult = await service.GetPagedAsync(null, "3", null, null, null, 1, 10);

            // Act 3: Lọc với chuỗi có khoảng trắng và ID không tồn tại "1, 999, abc"
            var safeResult = await service.GetPagedAsync(null, " 1 , 999, abc ", null, null, null, 1, 10);

            // Assert
            multiResult.TotalRecords.Should().Be(2);
            multiResult.Items.Select(x => x.Code).Should().Contain(new[] { "NCC01", "NCC02" });

            singleResult.TotalRecords.Should().Be(1);
            singleResult.Items.First().Code.Should().Be("NCC03");

            safeResult.TotalRecords.Should().Be(1);
            safeResult.Items.First().Code.Should().Be("NCC01");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT KÈM PHÂN LOẠI VÀ DANH SÁCH ĐỊA CHỈ
        /// <summary>
        /// TC03: Lấy chi tiết nhà cung cấp bao gồm cả tên Phân loại và các Địa chỉ kho trực thuộc.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnDetailedSupplier_WithAddresses()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var supplier = new Supplier
            {
                Id = 1,
                Code = "NCC01",
                Name = "Công ty TNHH Nông Sản Xanh",
                Phone = "0912345678",
                Email = "nongsanxanh@gmail.com",
                SupplierTypeId = 1,
                SupplierType = new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn" },
                Addresses = new List<SupplierAddress>
                {
                    new SupplierAddress
                    {
                        Id = 10,
                        ContactName = "Nguyễn Văn A",
                        ContactPhone = "0912345678",
                        StreetAddress = "123 Đường Số 1",
                        Ward = "Phường 1",
                        District = "Quận 1",
                        Province = "TP. Hồ Chí Minh",
                        IsDefault = true
                    }
                }
            };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Code.Should().Be("NCC01");
            result.SupplierTypeName.Should().Be("Nhà vườn");
            result.Addresses.Should().NotBeNullOrEmpty();
            result.Addresses!.First().ContactName.Should().Be("Nguyễn Văn A");
            result.Addresses!.First().IsDefault.Should().BeTrue();
        }
        #endregion

        #region TC04: TẠO MỚI NHÀ CUNG CẤP THÀNH CÔNG (KÈM TỰ ĐỘNG GÁN ĐỊA CHỈ DEFAULT)
        /// <summary>
        /// TC04: Tạo mới nhà cung cấp thành công. Nếu tạo kèm danh sách địa chỉ thì địa chỉ đầu tiên tự động nhận cờ IsDefault = true.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_AndSetFirstAddressDefault()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierTypes.Add(new SupplierType { Id = 1, Code = "FARM", Name = "Nhà vườn" });
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            var createDto = new SupplierCreateDto
            {
                Code = "  ncc_dalat  ",
                Name = "  Nông Trại Dâu Tây  ",
                Phone = " 0933112233 ",
                Email = " Dautay@GMAIL.COM ",
                SupplierTypeId = 1,
                IsActive = true,
                Addresses = new List<SupplierAddressCreateDto>
                {
                    new SupplierAddressCreateDto
                    {
                        ContactName = "Chị Lan",
                        ContactPhone = "0933112233",
                        StreetAddress = "Khu Phố 5",
                        Ward = "Phường 7",
                        District = "TP. Đà Lạt",
                        Province = "Lâm Đồng",
                        IsDefault = false // Cố tình truyền false để kiểm tra hệ thống tự động gán true
                    }
                }
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.Suppliers
                .Include(s => s.Addresses)
                .FirstOrDefaultAsync(s => s.Id == newId);

            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("NCC_DALAT"); // Viết hoa
            createdEntity.Email.Should().Be("dautay@gmail.com"); // Viết thường
            createdEntity.Name.Should().Be("Nông Trại Dâu Tây");
            createdEntity.Addresses.Should().HaveCount(1);
            createdEntity.Addresses.First().IsDefault.Should().BeTrue(); // Đã được tự động gán true
        }
        #endregion

        #region TC05: CHẶN TRÙNG MÃ CODE & SỐ ĐIỆN THOẠI
        /// <summary>
        /// TC05: Ném InvalidOperationException khi cố tình tạo mới với mã Code hoặc Số điện thoại đã tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCodeOrPhone()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "NCC01",
                Name = "Nhà cung cấp 1",
                Phone = "0901234567",
                Email = "ncc01@gmail.com"
            });
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            var duplicateCodeDto = new SupplierCreateDto { Code = "ncc01", Name = "Tên khác", Phone = "0999999999", Email = "test@gmail.com" };
            var duplicatePhoneDto = new SupplierCreateDto { Code = "NCC99", Name = "Tên khác", Phone = "0901234567", Email = "test2@gmail.com" };

            // Act & Assert 1: Trùng mã Code
            var actCode = () => service.CreateAsync(duplicateCodeDto);
            await actCode.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã nhà cung cấp 'ncc01' đã tồn tại trên hệ thống.*");

            // Act & Assert 2: Trùng Số điện thoại
            var actPhone = () => service.CreateAsync(duplicatePhoneDto);
            await actPhone.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Số điện thoại '0901234567' đã được đăng ký bởi nhà cung cấp khác.*");
        }
        #endregion

        #region TC06: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC06: Cập nhật thông tin nhà cung cấp thành công, chặn cập nhật nếu Code/Phone bị trùng với nhà cung cấp khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicate()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.AddRange(
                new Supplier { Id = 1, Code = "NCC01", Name = "Nhà cung cấp 1", Phone = "0901111111", Email = "ncc1@gmail.com" },
                new Supplier { Id = 2, Code = "NCC02", Name = "Nhà cung cấp 2", Phone = "0902222222", Email = "ncc2@gmail.com" }
            );
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act 1: Cập nhật hợp lệ cho ID = 1
            var updateDto = new SupplierUpdateDto
            {
                Code = "NCC01_NEW",
                Name = "Tên Mới 1",
                Phone = "0901111111",
                Email = "newemail@gmail.com",
                IsActive = true
            };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với ID = 2
            var duplicateDto = new SupplierUpdateDto
            {
                Code = "NCC02",
                Name = "Tên bất kỳ",
                Phone = "0909999999",
                Email = "dup@gmail.com",
                IsActive = true
            };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updatedEntity = await context.Suppliers.FindAsync(1);
            updatedEntity!.Code.Should().Be("NCC01_NEW");
            updatedEntity.Email.Should().Be("newemail@gmail.com");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã nhà cung cấp 'NCC02' đã được sử dụng bởi đơn vị khác.*");
        }
        #endregion

        #region TC07: SAFETY SHIELD 1 - CHẶN XÓA KHI CÓ ĐƠN MUA HÀNG (PO)
        /// <summary>
        /// TC07: Đảm bảo toàn vẹn dữ liệu chuỗi cung ứng: Chặn xóa NCC nếu đã có Đơn mua hàng (PO) liên kết.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasPurchaseOrders()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var supplier = new Supplier
            {
                Id = 1,
                Code = "NCC01",
                Name = "Nhà cung cấp A",
                Phone = "0901",
                Email = "a@gmail.com",
                PurchaseOrders = new List<PurchaseOrder>
                {
                    new PurchaseOrder
                    {
                        Id = 100,
                        OrderCode = "PO-202608-001",
                        SupplierId = 1,
                        IsDeleted = false
                    }
                }
            };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa nhà cung cấp này vì đã có Đơn mua hàng (PO) liên kết.*");
        }
        #endregion

        #region TC08: SAFETY SHIELD 2 - CHẶN XÓA KHI CÓ LÔ HÀNG (BATCH) TRONG KHO
        /// <summary>
        /// TC08: Đảm bảo toàn vẹn dữ liệu tồn kho: Chặn xóa NCC nếu đã có Lô hàng (Batch) liên kết.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasBatches()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var supplier = new Supplier
            {
                Id = 1,
                Code = "NCC01",
                Name = "Nhà cung cấp B",
                Phone = "0901",
                Email = "b@gmail.com",
                Batches = new List<ProductBatch>
                {
                    new ProductBatch
                    {
                        Id = 50,
                        BatchCode = "BATCH-AVOCADO-001",
                        VariantId = 1,
                        SupplierId = 1,
                        IsDeleted = false
                    }
                }
            };
            context.Suppliers.Add(supplier);
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa nhà cung cấp này vì đã có Lô hàng (Batch) liên kết trong kho.*");
        }
        #endregion

        #region TC09: XÓA THÀNH CÔNG KHI KHÔNG CÓ RÀNG BUỘC
        /// <summary>
        /// TC09: Xóa thành công nhà cung cấp độc lập không có dữ liệu ràng buộc (Soft Delete).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldDeleteSuccessfully_WhenNoConstraints()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "NCC_TEMP",
                Name = "Nhà cung cấp tạm thời",
                Phone = "0909999999",
                Email = "temp@gmail.com"
            });
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedEntity = await context.Suppliers.FindAsync(1);
            deletedEntity!.IsDeleted.Should().BeTrue();
            deletedEntity.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC10: CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC10: Đổi trạng thái hoạt động (Hoạt động -> Tạm khóa) của nhà cung cấp.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleStatusCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "NCC01",
                Name = "Nhà cung cấp 1",
                Phone = "0901",
                Email = "ncc1@gmail.com",
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new SupplierService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.Suppliers.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
