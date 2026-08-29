using AutoMapper;
using backend.DTOs.ProductDTOs;
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
    /// 📦 MODULE 5: PRODUCT & PRICING
    /// 🧪 UNIT TEST: ProductService (Quản lý Sản Phẩm Gốc / Master)
    /// ============================================================================
    /// </summary>
    public class ProductServiceTests
    {
        private readonly IMapper _mapper;

        public ProductServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM ĐA TIÊU CHÍ (TỪ KHÓA, ĐA DANH MỤC, ĐA ĐVT, TRẠNG THÁI, NGÀY TẠO/CẬP NHẬT)
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo từ khóa (Code/Name), lọc đa danh mục (categoryId: "1, 2, invalid"), 
        /// lọc đa ĐVT (baseUoMId: "10, 20"), trạng thái IsActive, và ngày tạo/cập nhật.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true },
                new UoM { Id = 2, Code = "BOX", Name = "Hộp", IsActive = true }
            );

            context.ProductCategories.AddRange(
                new ProductCategory { Id = 10, Code = "VEG", Name = "Rau xanh", IsActive = true },
                new ProductCategory { Id = 20, Code = "FRUIT", Name = "Trái cây", IsActive = true }
            );

            context.Products.AddRange(
                new Product
                {
                    Id = 100,
                    Code = "PROD-CACHUA",
                    Name = "Cà chua bi Đà Lạt",
                    Slug = "ca-chua-bi-da-lat",
                    CategoryId = 10,
                    BaseUoMId = 1,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 101,
                    Code = "PROD-TAO",
                    Name = "Táo Envy New Zealand",
                    Slug = "tao-envy-new-zealand",
                    CategoryId = 20,
                    BaseUoMId = 2,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = 102,
                    Code = "PROD-XALACH",
                    Name = "Xà lách thủy canh",
                    Slug = "xa-lach-thuy-canh",
                    CategoryId = 10,
                    BaseUoMId = 1,
                    IsActive = false,
                    CreatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc)
                }
            );
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "cà chua"
            var searchResult = await service.GetPagedAsync("cà chua", null, null, null, null, null, 1, 10);

            // Act 2: Lọc theo chuỗi CategoryId "10, invalid"
            var categoryFilterResult = await service.GetPagedAsync(null, "10, invalid", null, null, null, null, 1, 10);

            // Act 3: Lọc theo chuỗi BaseUoMId "2, invalid"
            var uomFilterResult = await service.GetPagedAsync(null, null, "2, invalid", null, null, null, 1, 10);

            // Act 4: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, null, false, null, null, 1, 10);

            // Act 5: Lọc theo ngày tạo
            var dateResult = await service.GetPagedAsync(null, null, null, null, new DateTime(2026, 8, 21), null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("PROD-CACHUA");

            categoryFilterResult.TotalRecords.Should().Be(2); // IDs 100 & 102
            uomFilterResult.TotalRecords.Should().Be(1); // ID 101

            statusResult.TotalRecords.Should().Be(1); // ID 102
            dateResult.TotalRecords.Should().Be(1); // ID 101
        }
        #endregion

        #region TC02: LẤY TOÀN BỘ DANH SÁCH CHO DROPDOWN (ALL LIST)
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách sản phẩm (kèm CategoryName và BaseUoMName), hỗ trợ cờ isActiveOnly.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnProductsWithNavigationProperties()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "VEG", Name = "Rau xanh hữu cơ" });

            context.Products.AddRange(
                new Product { Id = 1, Code = "P1", Name = "Sản phẩm A", CategoryId = 10, BaseUoMId = 1, IsActive = true },
                new Product { Id = 2, Code = "P2", Name = "Sản phẩm B", CategoryId = 10, BaseUoMId = 1, IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act
            var all = await service.GetAllListAsync(false);
            var activeOnly = await service.GetAllListAsync(true);

            // Assert
            all.Should().HaveCount(2);
            all.First().CategoryName.Should().Be("Rau xanh hữu cơ");
            all.First().BaseUoMName.Should().Be("Kilogram");

            activeOnly.Should().HaveCount(1);
            activeOnly.First().Code.Should().Be("P1");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID (GET BY ID)
        /// <summary>
        /// TC03: Lấy chi tiết sản phẩm theo ID hợp lệ và ném KeyNotFoundException khi không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnProduct_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "FRUIT", Name = "Trái cây" });
            context.Products.Add(new Product { Id = 50, Code = "P50", Name = "Xoài Cát Hòa Lộc", CategoryId = 10, BaseUoMId = 1 });
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(50);
            var actInvalid = () => service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid.Code.Should().Be("P50");
            valid.Name.Should().Be("Xoài Cát Hòa Lộc");
            valid.CategoryName.Should().Be("Trái cây");
            valid.BaseUoMName.Should().Be("Kilogram");

            await actInvalid.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy sản phẩm*");
        }
        #endregion

        #region TC04: TẠO MỚI SẢN PHẨM THÀNH CÔNG (CREATE PRODUCT)
        /// <summary>
        /// TC04: Tạo mới sản phẩm gốc thành công, tự động cắt tỉa khoảng trắng và khởi tạo CreatedAt/UpdatedAt.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithValidData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "VEG", Name = "Rau xanh", IsActive = true });
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            var dto = new ProductCreateDto
            {
                Code = "  PROD-DUALEO  ",
                Name = "  Dưa leo baby Đà Lạt  ",
                Description = "  Trồng thủy canh đạt chuẩn VietGAP  ",
                ImagePath = "  /images/dualeo.png  ",
                CategoryId = 10,
                BaseUoMId = 1,
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.Products.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("PROD-DUALEO");
            createdEntity.Name.Should().Be("Dưa leo baby Đà Lạt");
            createdEntity.Description.Should().Be("Trồng thủy canh đạt chuẩn VietGAP");
            createdEntity.ImagePath.Should().Be("/images/dualeo.png");
            createdEntity.CategoryId.Should().Be(10);
            createdEntity.BaseUoMId.Should().Be(1);
            createdEntity.IsActive.Should().BeTrue();
            createdEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
        #endregion

        #region TC05: BẮT LỖI VALIDATION KHI TẠO MỚI (TRÙNG MÃ, SAI BASE UOM, SAI CATEGORY)
        /// <summary>
        /// TC05: Ném InvalidOperationException khi mã code bị trùng lặp, BaseUoMId không tồn tại hoặc CategoryId không tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenValidationFails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "VEG", Name = "Rau xanh" });
            context.Products.Add(new Product { Id = 1, Code = "PROD-EXISTING", Name = "Sản phẩm cũ", BaseUoMId = 1, CategoryId = 10 });
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act & Assert 1: Trùng mã sản phẩm (case-insensitive)
            var duplicateCodeDto = new ProductCreateDto { Code = "prod-existing", Name = "Sản phẩm trùng", BaseUoMId = 1, CategoryId = 10 };
            var actDuplicate = () => service.CreateAsync(duplicateCodeDto);
            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã sản phẩm 'prod-existing' đã tồn tại trong hệ thống.*");

            // Act & Assert 2: Đơn vị tính cơ sở không tồn tại
            var invalidUoMDto = new ProductCreateDto { Code = "PROD-NEW1", Name = "SP mới", BaseUoMId = 999, CategoryId = 10 };
            var actInvalidUoM = () => service.CreateAsync(invalidUoMDto);
            await actInvalidUoM.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Đơn vị tính cơ sở không tồn tại*");

            // Act & Assert 3: Danh mục sản phẩm không tồn tại
            var invalidCatDto = new ProductCreateDto { Code = "PROD-NEW2", Name = "SP mới", BaseUoMId = 1, CategoryId = 999 };
            var actInvalidCat = () => service.CreateAsync(invalidCatDto);
            await actInvalidCat.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Danh mục sản phẩm không tồn tại*");
        }
        #endregion

        #region TC06: CẬP NHẬT SẢN PHẨM & BẮT LỖI RÀNG BUỘC (UPDATE PRODUCT)
        /// <summary>
        /// TC06: Cập nhật thông tin sản phẩm thành công, ném KeyNotFoundException khi ID không tồn tại, 
        /// ném InvalidOperationException khi trùng mã với sản phẩm khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndValidateUniqueness()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "VEG", Name = "Rau xanh" });
            context.Products.AddRange(
                new Product { Id = 1, Code = "P1", Name = "Sản phẩm 1", BaseUoMId = 1, CategoryId = 10 },
                new Product { Id = 2, Code = "P2", Name = "Sản phẩm 2", BaseUoMId = 1, CategoryId = 10 }
            );
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new ProductUpdateDto
            {
                Code = "P1",
                Name = "Sản phẩm 1 (Đã đổi tên)",
                Description = "Mô tả mới",
                BaseUoMId = 1,
                CategoryId = 10,
                IsActive = true
            };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với Product ID = 2 (P2)
            var duplicateDto = new ProductUpdateDto { Code = "p2", Name = "Tên bất kỳ", BaseUoMId = 1 };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Act 3: Cập nhật ID không tồn tại
            var actNotFound = () => service.UpdateAsync(999, updateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updatedEntity = await context.Products.FindAsync(1);
            updatedEntity!.Name.Should().Be("Sản phẩm 1 (Đã đổi tên)");
            updatedEntity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cập nhật thất bại: Mã sản phẩm 'p2' đã bị trùng lặp.*");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy sản phẩm cần sửa.*");
        }
        #endregion

        #region TC07: SAFETY SHIELD - CHẶN XÓA KHI CÓ BIẾN THỂ HOẶC QUY TẮC QUY ĐỔI
        /// <summary>
        /// TC07: Kiểm tra lá chắn an toàn (Safety Shield): Chặn xóa sản phẩm khi đang có biến thể SKU trực thuộc
        /// hoặc đang có quy tắc quy đổi đơn vị tính đặc thù.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasVariantsOrConversions()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.Products.AddRange(
                new Product { Id = 1, Code = "P_VAR", Name = "SP có biến thể", BaseUoMId = 1 },
                new Product { Id = 2, Code = "P_CONV", Name = "SP có quy đổi", BaseUoMId = 1 }
            );

            context.ProductVariants.Add(new ProductVariant { Id = 10, Code = "SKU-01", Name = "Biến thể 1", ProductId = 1, IsDeleted = false });
            context.UoMConversions.Add(new UoMConversion { Id = 20, ProductId = 2, FromUoMId = 1, ToUoMId = 1, ConversionFactor = 10, IsDeleted = false });
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act & Assert 1: Chặn xóa do có biến thể
            var actVar = () => service.DeleteAsync(1);
            await actVar.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa sản phẩm này vì đang có các biến thể (SKU) trực thuộc.*");

            // Act & Assert 2: Chặn xóa do có quy tắc quy đổi
            var actConv = () => service.DeleteAsync(2);
            await actConv.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa sản phẩm này vì đang có quy tắc quy đổi đơn vị liên kết.*");
        }
        #endregion

        #region TC08: XÓA THÀNH CÔNG VÀ CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG
        /// <summary>
        /// TC08: Xóa sản phẩm thành công khi không có ràng buộc dữ liệu và kiểm tra tính năng Toggle Active.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_And_ToggleActiveAsync_ShouldWorkCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.Products.Add(new Product { Id = 1, Code = "P_ALONE", Name = "SP độc lập", BaseUoMId = 1, IsActive = true });
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act 1: Toggle Active
            await service.ToggleActiveAsync(1);
            var entityAfterToggle = await context.Products.FindAsync(1);
            entityAfterToggle!.IsActive.Should().BeFalse();

            // Act 2: Xóa thành công
            var deleteResult = await service.DeleteAsync(1);
            var entityAfterDelete = await context.Products.FindAsync(1);

            // Assert
            deleteResult.Should().BeTrue();
            entityAfterDelete!.IsDeleted.Should().BeTrue();
            entityAfterDelete.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC09: LẤY CẤU HÌNH THUỘC TÍNH ĐỘNG THEO DANH MỤC (EAV TEMPLATE)
        /// <summary>
        /// TC09: Lấy cấu hình các thuộc tính động (CategoryAttribute Template) cần nhập cho sản phẩm theo danh mục trực thuộc.
        /// </summary>
        [Fact]
        public async Task GetDynamicAttributesConfigAsync_ShouldReturnConfiguredAttributes()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram" });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "FRUIT", Name = "Trái cây" });
            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 100, Name = "Độ ngọt Brix", DataType = "NUMBER", IsActive = true },
                new AttributeDefinition { Id = 101, Name = "Vùng trồng", DataType = "TEXT", IsActive = true },
                new AttributeDefinition { Id = 102, Name = "Thuộc tính ẩn", DataType = "TEXT", IsActive = false }
            );

            context.CategoryAttributes.AddRange(
                new CategoryAttribute { Id = 1, CategoryId = 10, AttributeDefinitionId = 100, IsRequired = true },
                new CategoryAttribute { Id = 2, CategoryId = 10, AttributeDefinitionId = 101, IsRequired = false },
                new CategoryAttribute { Id = 3, CategoryId = 10, AttributeDefinitionId = 102, IsRequired = false } // Thuộc tính inactive
            );

            context.Products.Add(new Product { Id = 1, Code = "P_MANGO", Name = "Xoài Cát", CategoryId = 10, BaseUoMId = 1 });
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mapper);

            // Act
            var configs = (await service.GetDynamicAttributesConfigAsync(1)).ToList();

            // Assert
            configs.Should().HaveCount(2); // Only active attributes (100 & 101)
        }
        #endregion
    }
}
