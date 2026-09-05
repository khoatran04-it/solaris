using AutoMapper;
using backend.DTOs.ProductVariantDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module05_ProductPricing
{
    /// <summary>
    /// ============================================================================
    /// MODULE 5: PRODUCT & PRICING
    /// UNIT TEST: ProductVariantService (Quản lý Biến Thể Sản Phẩm - SKU & Bảng Giá)
    /// ============================================================================
    /// </summary>
    public class ProductVariantServiceTests
    {
        private readonly IMapper _mapper;

        public ProductVariantServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM ĐA TIÊU CHÍ (TỪ KHÓA, ĐA SẢN PHẨM CHA, TRẠNG THÁI, NGÀY)
        /// <summary>
        /// TC01: Kiểm tra tìm kiếm theo mã SKU / Tên biến thể, lọc đa sản phẩm cha ("1, 2, invalid"), 
        /// lọc theo trạng thái IsActive và ngày tạo.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Products.AddRange(
                new Product { Id = 1, Code = "P1", Name = "Táo Envy", BaseUoMId = 1 },
                new Product { Id = 2, Code = "P2", Name = "Cà chua bi", BaseUoMId = 1 }
            );

            context.ProductVariants.AddRange(
                new ProductVariant
                {
                    Id = 10,
                    Code = "SKU-TAO-1KG",
                    Name = "Táo Envy Túi 1Kg",
                    ProductId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = 11,
                    Code = "SKU-TAO-THUNG",
                    Name = "Táo Envy Thùng 10Kg",
                    ProductId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc)
                },
                new ProductVariant
                {
                    Id = 12,
                    Code = "SKU-CACHUA-500G",
                    Name = "Cà chua bi Hộp 500g",
                    ProductId = 2,
                    IsActive = false,
                    CreatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc)
                }
            );
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "thùng"
            var searchResult = await service.GetPagedAsync("thùng", null, null, null, null, 1, 10);

            // Act 2: Lọc theo danh sách ProductId "1, invalid"
            var productFilterResult = await service.GetPagedAsync(null, "1, invalid", null, null, null, 1, 10);

            // Act 3: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("SKU-TAO-THUNG");

            productFilterResult.TotalRecords.Should().Be(2); // IDs 10 & 11
            statusResult.TotalRecords.Should().Be(1); // ID 12
        }
        #endregion

        #region TC02: LẤY DANH SÁCH & TÍNH TOÁN GIÁ KHUYẾN MÃI (APPLY PROMOTIONS)
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách biến thể, tự động tính toán PromotionalPrice cho các bảng giá dựa trên Campaign giảm giá % hoặc giảm tiền trực tiếp đang diễn ra.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldApplyPromotionsCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "KG", Name = "Kilogram" },
                new UoM { Id = 2, Code = "BOX", Name = "Hộp" }
            );

            context.Products.Add(new Product { Id = 1, Code = "P1", Name = "Bơ 034", BaseUoMId = 1 });

            var variant = new ProductVariant
            {
                Id = 10,
                Code = "SKU-BO-1KG",
                Name = "Bơ 034 Loại 1 Túi 1Kg",
                ProductId = 1,
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            // Thêm bảng giá: 100,000 VND / Kg
            context.ProductVariantPrices.Add(new ProductVariantPrice
            {
                Id = 100,
                VariantId = 10,
                UoMId = 1,
                Price = 100000m,
                IsDefault = true,
                IsActive = true
            });

            // Thêm chiến dịch khuyến mãi giảm 20%
            var now = DateTime.UtcNow;
            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Flash Sale 20%",
                IsPercentage = true,
                DiscountValue = 20, // 20%
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(2),
                IsActive = true
            };
            context.PromotionCampaigns.Add(campaign);

            context.PromotionVariants.Add(new PromotionVariant
            {
                Id = 1,
                PromotionCampaignId = 1,
                VariantId = 10
            });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act
            var list = (await service.GetAllListAsync(false)).ToList();

            // Assert
            list.Should().HaveCount(1);
            var item = list.First();
            item.Prices.Should().HaveCount(1);
            var price = item.Prices.First();
            price.Price.Should().Be(100000m);
            price.PromotionalPrice.Should().Be(80000m); // 100,000 * (1 - 0.20) = 80,000
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID (KÈM BẢNG GIÁ & THUỘC TÍNH EAV)
        /// <summary>
        /// TC03: Lấy thông tin chi tiết một SKU theo ID, bao gồm danh sách bảng giá và danh sách thuộc tính động EAV.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnVariantWithPricesAndAttributes()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 5, Name = "Vùng trồng", DataType = "TEXT" });

            context.Products.Add(new Product { Id = 1, Code = "P1", Name = "Sầu riêng Ri6", BaseUoMId = 1 });

            var variant = new ProductVariant
            {
                Id = 10,
                Code = "SKU-SAURIENG",
                Name = "Sầu riêng Ri6 Nguyên Trái",
                ProductId = 1,
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            context.ProductVariantPrices.Add(new ProductVariantPrice
            {
                Id = 1,
                VariantId = 10,
                UoMId = 1,
                Price = 150000m,
                IsDefault = true,
                IsActive = true
            });

            context.ProductAttributes.Add(new ProductAttribute
            {
                Id = 1,
                VariantId = 10,
                AttributeDefinitionId = 5,
                AttributeValue = "Bến Tre",
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(10);
            var actNotFound = () => service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid.Code.Should().Be("SKU-SAURIENG");
            valid.ProductName.Should().Be("Sầu riêng Ri6");
            valid.Prices.Should().HaveCount(1);
            valid.Prices.First().UoMName.Should().Be("Kilogram");
            valid.Attributes.Should().HaveCount(1);
            valid.Attributes.First().AttributeDefinitionName.Should().Be("Vùng trồng");
            valid.Attributes.First().AttributeValue.Should().Be("Bến Tre");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy biến thể sản phẩm.*");
        }
        #endregion

        #region TC04: TẠO MỚI BIẾN THỂ & BẮT LỖI VALIDATION (TRÙNG SKU, SAI PRODUCT ID)
        /// <summary>
        /// TC04: Tạo mới SKU thành công, ném InvalidOperationException khi trùng mã SKU hoặc ProductId không tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_AndValidateUniqueness()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.Products.Add(new Product { Id = 1, Code = "P1", Name = "Xoài Cát Chu", BaseUoMId = 1 });
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "SKU-EXISTING", Name = "SKU Cũ", ProductId = 1 });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act 1: Tạo mới hợp lệ
            var createDto = new ProductVariantCreateDto
            {
                Code = "  SKU-XOAI-1KG  ",
                Name = "  Xoài Cát Chu Túi 1Kg  ",
                Description = "  Xoài ngọt chuẩn VietGAP  ",
                ImagePath = "  /images/xoai.png  ",
                ProductId = 1,
                IsActive = true,
                Prices = new List<VariantPriceInputDto>
                {
                    new VariantPriceInputDto { UoMId = 1, Price = 65000m, IsDefault = true }
                },
                Attributes = new List<AttributeInputDto>
                {
                    new AttributeInputDto { AttributeDefinitionId = 1, AttributeValue = "Đồng Tháp" }
                }
            };

            var newId = await service.CreateAsync(createDto);

            // Act 2: Trùng mã SKU (case-insensitive)
            var duplicateDto = new ProductVariantCreateDto { Code = "sku-existing", Name = "SKU trùng", ProductId = 1 };
            var actDuplicate = () => service.CreateAsync(duplicateDto);

            // Act 3: ProductId không tồn tại
            var invalidProductDto = new ProductVariantCreateDto { Code = "SKU-NEW", Name = "Mới", ProductId = 999 };
            var actInvalidProduct = () => service.CreateAsync(invalidProductDto);

            // Assert
            newId.Should().BeGreaterThan(0);
            var created = await context.ProductVariants
                .Include(x => x.Prices)
                .Include(x => x.Attributes)
                .FirstOrDefaultAsync(x => x.Id == newId);

            created.Should().NotBeNull();
            created!.Code.Should().Be("SKU-XOAI-1KG");
            created.Name.Should().Be("Xoài Cát Chu Túi 1Kg");
            created.Prices.Should().HaveCount(1);
            created.Prices.First().Price.Should().Be(65000m);
            created.Attributes.Should().HaveCount(1);

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã SKU 'sku-existing' của biến thể này đã tồn tại*");

            await actInvalidProduct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Sản phẩm gốc không tồn tại*");
        }
        #endregion

        #region TC05: CẬP NHẬT BIẾN THỂ & CASCADING UPDATE BẢNG GIÁ / THUỘC TÍNH
        /// <summary>
        /// TC05: Cập nhật biến thể, xóa sạch bảng giá và thuộc tính cũ để chèn danh sách mới nhất quán.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndReplacePricesAndAttributes()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "KG", Name = "Kilogram" },
                new UoM { Id = 2, Code = "BOX", Name = "Hộp" }
            );

            context.Products.Add(new Product { Id = 1, Code = "P1", Name = "Dưa hấu", BaseUoMId = 1 });

            var variant = new ProductVariant
            {
                Id = 10,
                Code = "SKU-DUAHAU",
                Name = "Dưa hấu không hạt",
                ProductId = 1,
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            // Thêm giá và thuộc tính cũ
            context.ProductVariantPrices.Add(new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 30000m, IsActive = true });
            context.ProductAttributes.Add(new ProductAttribute { Id = 1, VariantId = 10, AttributeDefinitionId = 1, AttributeValue = "Long An", IsActive = true });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act: Cập nhật sang bảng giá và thuộc tính mới
            var updateDto = new ProductVariantUpdateDto
            {
                Code = "SKU-DUAHAU",
                Name = "Dưa hấu không hạt Mặt Trời Đỏ",
                ProductId = 1,
                IsActive = true,
                Prices = new List<VariantPriceInputDto>
                {
                    new VariantPriceInputDto { UoMId = 2, Price = 50000m, IsDefault = true }
                },
                Attributes = new List<AttributeInputDto>
                {
                    new AttributeInputDto { AttributeDefinitionId = 2, AttributeValue = "Hữu cơ" }
                }
            };

            var result = await service.UpdateAsync(10, updateDto);

            // Assert
            result.Should().BeTrue();

            var updatedVariant = await context.ProductVariants
                .Include(x => x.Prices)
                .Include(x => x.Attributes)
                .FirstOrDefaultAsync(x => x.Id == 10);

            updatedVariant!.Name.Should().Be("Dưa hấu không hạt Mặt Trời Đỏ");
            var activePrices = updatedVariant.Prices.Where(p => !p.IsDeleted).ToList();
            activePrices.Should().HaveCount(1);
            activePrices.First().UoMId.Should().Be(2);
            activePrices.First().Price.Should().Be(50000m);

            var activeAttrs = updatedVariant.Attributes.Where(a => !a.IsDeleted).ToList();
            activeAttrs.Should().HaveCount(1);
            activeAttrs.First().AttributeDefinitionId.Should().Be(2);
            activeAttrs.First().AttributeValue.Should().Be("Hữu cơ");
        }
        #endregion

        #region TC06: SAFETY SHIELD - CHẶN XÓA KHI CÓ LÔ HÀNG / BẢNG GIÁ NCC / PO / SO / PHIẾU KHO
        /// <summary>
        /// TC06: Kiểm tra 6 lá chắn an toàn (Safety Shields) chặn xóa SKU khi đã phát sinh dữ liệu liên kết.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenReferencedByOtherEntities()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.AddRange(
                new ProductVariant { Id = 1, Code = "V1", Name = "Var 1", ProductId = 1 },
                new ProductVariant { Id = 2, Code = "V2", Name = "Var 2", ProductId = 1 },
                new ProductVariant { Id = 3, Code = "V3", Name = "Var 3", ProductId = 1 },
                new ProductVariant { Id = 4, Code = "V4", Name = "Var 4", ProductId = 1 },
                new ProductVariant { Id = 5, Code = "V5", Name = "Var 5", ProductId = 1 },
                new ProductVariant { Id = 6, Code = "V6", Name = "Var 6", ProductId = 1 }
            );

            // 1. Có Lô hàng
            context.ProductBatches.Add(new ProductBatch { Id = 1, VariantId = 1, BatchCode = "BATCH01", IsDeleted = false });
            // 2. Có Bảng giá NCC
            context.SupplierProducts.Add(new SupplierProduct { Id = 1, VariantId = 2, SupplierId = 1, PurchaseUoMId = 1, IsDeleted = false });
            // 3. Có Đơn mua hàng (PO)
            context.PurchaseOrderDetails.Add(new PurchaseOrderDetail { Id = 1, PurchaseOrderId = 1, VariantId = 3, UoMId = 1, OrderQuantity = 10, UnitPrice = 1000, TotalPrice = 10000 });
            // 4. Có Đơn bán hàng (SO)
            context.OrderDetails.Add(new OrderDetail { Id = 1, OrderId = 1, VariantId = 4, UoMId = 1, Quantity = 5, UnitPrice = 2000, TotalPrice = 10000 });
            // 5. Có Phiếu nhập kho (IR)
            context.InventoryReceiptDetails.Add(new InventoryReceiptDetail { Id = 1, InventoryReceiptId = 1, VariantId = 5, BatchId = 1, UoMId = 1, ExpectedQuantity = 10, AcceptedQuantity = 10 });
            // 6. Có Phiếu xuất kho (II)
            context.InventoryIssueDetails.Add(new InventoryIssueDetail { Id = 1, InventoryIssueId = 1, VariantId = 6, BatchId = 1, UoMId = 1, Quantity = 2, UnitPrice = 5000, TotalPrice = 10000 });

            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act & Assert
            var act1 = () => service.DeleteAsync(1);
            await act1.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đang có các lô hàng liên kết.*");

            var act2 = () => service.DeleteAsync(2);
            await act2.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đang có bảng giá nhà cung cấp liên kết.*");

            var act3 = () => service.DeleteAsync(3);
            await act3.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã phát sinh trong đơn mua hàng (PO).*");

            var act4 = () => service.DeleteAsync(4);
            await act4.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã phát sinh trong đơn bán hàng.*");

            var act5 = () => service.DeleteAsync(5);
            await act5.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã phát sinh trong phiếu nhập kho.*");

            var act6 = () => service.DeleteAsync(6);
            await act6.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã phát sinh trong phiếu xuất kho.*");
        }
        #endregion

        #region TC07: XÓA THÀNH CÔNG VÀ CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG
        /// <summary>
        /// TC07: Xóa thành công khi không có ràng buộc và kiểm tra hàm ToggleActiveAsync.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_And_ToggleActiveAsync_ShouldWorkCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.Add(new ProductVariant { Id = 100, Code = "SKU-STANDALONE", Name = "SKU Độc lập", ProductId = 1, IsActive = true });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            // Act 1: Toggle Active
            await service.ToggleActiveAsync(100);
            var entityAfterToggle = await context.ProductVariants.FindAsync(100);
            entityAfterToggle!.IsActive.Should().BeFalse();

            // Act 2: Xóa thành công
            var deleteResult = await service.DeleteAsync(100);
            var entityAfterDelete = await context.ProductVariants.FindAsync(100);

            // Assert
            deleteResult.Should().BeTrue();
            entityAfterDelete!.IsDeleted.Should().BeTrue();
            entityAfterDelete.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC08 & TC09: QUY CÁCH ĐÓNG GÓI, KÍCH THƯỚC VÀ TÍNH TOÁN CBM
        /// <summary>
        /// TC08: Tạo mới biến thể tự động tính toán thể tích UnitCbm từ kích thước Dài, Rộng, Cao và lưu Khối lượng cả bì.
        /// </summary>
        [Fact]
        public async Task TC08_CreateAsync_ShouldCalculateUnitCbm_FromDimensions_AndPersistGrossWeight()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Products.Add(new Product { Id = 1, Code = "P-AVO", Name = "Bơ Sáp 034", BaseUoMId = 1 });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            var createDto = new ProductVariantCreateDto
            {
                ProductId = 1,
                Code = "SKU-AVO-BOX",
                Name = "Bơ Sáp Thùng 10Kg",
                LengthCm = 50,
                WidthCm = 40,
                HeightCm = 25, // 50x40x25 / 1,000,000 = 0.05 CBM
                GrossWeightKg = 10.5m,
                IsActive = true
            };

            // Act
            var variantId = await service.CreateAsync(createDto);

            // Assert
            var variant = await context.ProductVariants.FindAsync(variantId);
            variant.Should().NotBeNull();
            variant!.LengthCm.Should().Be(50);
            variant.WidthCm.Should().Be(40);
            variant.HeightCm.Should().Be(25);
            variant.UnitCbm.Should().Be(0.05m);
            variant.GrossWeightKg.Should().Be(10.5m);
        }

        /// <summary>
        /// TC09: Cập nhật biến thể với kích thước mới tự động tính toán lại thể tích UnitCbm.
        /// </summary>
        [Fact]
        public async Task TC09_UpdateAsync_ShouldRecalculateUnitCbm_WhenDimensionsChange()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.Products.Add(new Product { Id = 1, Code = "P-AVO", Name = "Bơ Sáp 034", BaseUoMId = 1 });
            context.ProductVariants.Add(new ProductVariant
            {
                Id = 50,
                ProductId = 1,
                Code = "SKU-AVO-50",
                Name = "Bơ Thùng Cũ",
                LengthCm = 40,
                WidthCm = 30,
                HeightCm = 20,
                UnitCbm = 0.024m,
                GrossWeightKg = 5m,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new ProductVariantService(context, _mapper);

            var updateDto = new ProductVariantUpdateDto
            {
                ProductId = 1,
                Code = "SKU-AVO-50",
                Name = "Bơ Thùng Mới",
                LengthCm = 100,
                WidthCm = 50,
                HeightCm = 40, // 100x50x40 / 1,000,000 = 0.2 CBM
                GrossWeightKg = 22m,
                IsActive = true
            };

            // Act
            var success = await service.UpdateAsync(50, updateDto);

            // Assert
            success.Should().BeTrue();
            var variant = await context.ProductVariants.FindAsync(50);
            variant!.UnitCbm.Should().Be(0.2m);
            variant.GrossWeightKg.Should().Be(22m);
        }
        #endregion
    }
}
