using AutoMapper;
using backend.DTOs.SupplierProductDTOs;
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
    /// UNIT TEST: SupplierProductService (Quản lý Bảng giá & Danh mục NCC)
    /// ============================================================================
    /// </summary>
    public class SupplierProductServiceTests
    {
        private readonly IMapper _mapper;

        public SupplierProductServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM ĐA NĂNG
        /// <summary>
        /// TC01: Tìm kiếm theo SupplierSKU, Tên biến thể, Tên NCC và phân trang.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldSearchAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var supplier = new Supplier { Id = 1, Code = "NCC01", Name = "Nông trại Bơ Sáp", Phone = "0901", Email = "bo@gmail.com" };
            var variant = new ProductVariant { Id = 10, Code = "BO_034", Name = "Bơ 034 Loại 1" };
            var uom = new UoM { Id = 100, Code = "KG", Name = "Kilogram", IsActive = true };

            context.Suppliers.Add(supplier);
            context.ProductVariants.Add(variant);
            context.UoMs.Add(uom);

            context.SupplierProducts.Add(new SupplierProduct
            {
                Id = 1,
                SupplierId = 1,
                VariantId = 10,
                PurchaseUoMId = 100,
                SupplierSKU = "SKU-BO-034",
                LastImportPrice = 35000,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act 1: Tìm kiếm theo SKU
            var skuResult = await service.GetPagedAsync("SKU-BO", null, null, null, null, null, 1, 10);

            // Act 2: Tìm kiếm theo tên biến thể
            var variantResult = await service.GetPagedAsync("Bơ 034", null, null, null, null, null, 1, 10);

            // Act 3: Tìm kiếm theo tên NCC
            var supplierResult = await service.GetPagedAsync("Bơ Sáp", null, null, null, null, null, 1, 10);

            // Assert
            skuResult.TotalRecords.Should().Be(1);
            skuResult.Items.First().SupplierSKU.Should().Be("SKU-BO-034");

            variantResult.TotalRecords.Should().Be(1);
            supplierResult.TotalRecords.Should().Be(1);
        }
        #endregion

        #region TC02: LẤY DANH SÁCH THEO SUPPLIER ID
        /// <summary>
        /// TC02: Lấy danh sách toàn bộ các mặt hàng được cung cấp bởi 1 nhà cung cấp cụ thể.
        /// </summary>
        [Fact]
        public async Task GetBySupplierIdAsync_ShouldReturnSupplierProducts()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "V01", Name = "Sản phẩm A" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true });

            context.SupplierProducts.Add(new SupplierProduct
            {
                Id = 1,
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                LastImportPrice = 8000,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act
            var results = (await service.GetBySupplierIdAsync(1)).ToList();

            // Assert
            results.Should().HaveCount(1);
            results.First().LastImportPrice.Should().Be(8000);
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết một liên kết sản phẩm - NCC theo ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "V01", Name = "Sản phẩm A" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true });

            context.SupplierProducts.Add(new SupplierProduct
            {
                Id = 1,
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                LastImportPrice = 8000
            });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act
            var validItem = await service.GetByIdAsync(1);
            var actInvalid = () => service.GetByIdAsync(999);

            // Assert
            validItem.Should().NotBeNull();
            validItem.LastImportPrice.Should().Be(8000);

            await actInvalid.Should().ThrowAsync<KeyNotFoundException>();
        }
        #endregion

        #region TC04: TẠO MỚI CẤU HÌNH GIÁ NHẬP THÀNH CÔNG
        /// <summary>
        /// TC04: Tạo mới liên kết giá nhập thành công và tự động viết hoa SupplierSKU.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedSKU()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "V01", Name = "Sản phẩm A" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            var createDto = new SupplierProductCreateDto
            {
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                SupplierSKU = "  sku-sp-a  ",
                LastImportPrice = 7500,
                MinimumOrderQuantity = 10,
                LeadTimeDays = 2,
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            newId.Should().BeGreaterThan(0);
            var entity = await context.SupplierProducts.FindAsync(newId);
            entity.Should().NotBeNull();
            entity!.SupplierSKU.Should().Be("SKU-SP-A"); // Tự động viết hoa và Trim
            entity.LastImportPrice.Should().Be(7500);
            entity.MinimumOrderQuantity.Should().Be(10);
            entity.LeadTimeDays.Should().Be(2);
        }
        #endregion

        #region TC05: ĐẶC THÙ - CHẶN TRÙNG LẶP CẶP (VARIANT_ID + SUPPLIER_ID)
        /// <summary>
        /// TC05: Đảm bảo nguyên tắc 1 NCC - 1 Biến thể chỉ có 1 dòng cấu hình giá: Chặn tạo trùng lặp cặp (VariantId + SupplierId).
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateVariantAndSupplier()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "V01", Name = "Sản phẩm A" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true });

            context.SupplierProducts.Add(new SupplierProduct
            {
                Id = 1,
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                LastImportPrice = 8000
            });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            var duplicateDto = new SupplierProductCreateDto
            {
                SupplierId = 1, // Đã tồn tại
                VariantId = 1,  // Đã tồn tại
                PurchaseUoMId = 1,
                LastImportPrice = 9000
            };

            // Act
            var act = () => service.CreateAsync(duplicateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Nhà cung cấp này đã có cấu hình giá cho sản phẩm được chọn.*");
        }
        #endregion

        #region TC06: CHẶN TẠO KHI THỰC THỂ LIÊN KẾT KHÔNG TỒN TẠI
        /// <summary>
        /// TC06: Ném InvalidOperationException nếu VariantId, SupplierId hoặc PurchaseUoMId không tồn tại trong hệ thống.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenForeignEntitiesDoNotExist()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "V01", Name = "Sản phẩm A" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act & Assert 1: Sai VariantId
            var actVariant = () => service.CreateAsync(new SupplierProductCreateDto { SupplierId = 1, VariantId = 999, PurchaseUoMId = 1 });
            await actVariant.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Biến thể sản phẩm được chọn không tồn tại.*");

            // Act & Assert 2: Sai SupplierId
            var actSupplier = () => service.CreateAsync(new SupplierProductCreateDto { SupplierId = 999, VariantId = 1, PurchaseUoMId = 1 });
            await actSupplier.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Nhà cung cấp được chọn không tồn tại.*");

            // Act & Assert 3: Sai PurchaseUoMId
            var actUom = () => service.CreateAsync(new SupplierProductCreateDto { SupplierId = 1, VariantId = 1, PurchaseUoMId = 999 });
            await actUom.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Đơn vị tính mua hàng được chọn không tồn tại.*");
        }
        #endregion

        #region TC07: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC07: Cập nhật giá nhập thành công và chặn sửa thành cặp (VariantId + SupplierId) của bản ghi khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicatePair()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Suppliers.Add(new Supplier { Id = 1, Code = "NCC01", Name = "NCC 1", Phone = "0901", Email = "ncc1@gmail.com" });
            context.ProductVariants.AddRange(
                new ProductVariant { Id = 1, Code = "V01", Name = "Sản phẩm A" },
                new ProductVariant { Id = 2, Code = "V02", Name = "Sản phẩm B" }
            );
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true });

            context.SupplierProducts.AddRange(
                new SupplierProduct { Id = 1, SupplierId = 1, VariantId = 1, PurchaseUoMId = 1, LastImportPrice = 8000 },
                new SupplierProduct { Id = 2, SupplierId = 1, VariantId = 2, PurchaseUoMId = 1, LastImportPrice = 15000 }
            );
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act 1: Cập nhật hợp lệ cho ID = 1
            var updateDto = new SupplierProductUpdateDto
            {
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                LastImportPrice = 8500,
                IsActive = true
            };
            var result = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật ID = 1 đổi sang VariantId = 2 (trùng với bản ghi ID = 2)
            var duplicateDto = new SupplierProductUpdateDto
            {
                SupplierId = 1,
                VariantId = 2,
                PurchaseUoMId = 1,
                LastImportPrice = 9000,
                IsActive = true
            };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Assert
            result.Should().BeTrue();
            var updatedEntity = await context.SupplierProducts.FindAsync(1);
            updatedEntity!.LastImportPrice.Should().Be(8500);

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cập nhật thất bại: Cấu hình liên kết giữa Sản phẩm và Nhà cung cấp này đã tồn tại.*");
        }
        #endregion

        #region TC08: XÓA MỀM CẤU HÌNH GIÁ NHẬP (SOFT DELETE)
        /// <summary>
        /// TC08: Xóa cấu hình giá nhập bằng cờ IsDeleted = true và cập nhật DeletedAt.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldSoftDeleteRecord()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierProducts.Add(new SupplierProduct
            {
                Id = 1,
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                LastImportPrice = 8000,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var entity = await context.SupplierProducts.FindAsync(1);
            entity!.IsDeleted.Should().BeTrue();
            entity.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC09: ĐỔI TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC09: Chuyển đổi trạng thái IsActive (true -> false).
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleStatusCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.SupplierProducts.Add(new SupplierProduct
            {
                Id = 1,
                SupplierId = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                LastImportPrice = 8000,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new SupplierProductService(context, _mapper);

            // Act
            await service.ToggleActiveAsync(1);

            // Assert
            var entity = await context.SupplierProducts.FindAsync(1);
            entity!.IsActive.Should().BeFalse();
        }
        #endregion
    }
}
