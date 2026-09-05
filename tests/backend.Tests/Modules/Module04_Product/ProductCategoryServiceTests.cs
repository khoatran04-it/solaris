using AutoMapper;
using backend.DTOs.ProductCategoryDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module04_Product
{
    /// <summary>
    /// ============================================================================
    /// MODULE 4: PRODUCT MASTER DATA
    /// UNIT TEST: ProductCategoryService (Quản lý Danh Mục Sản Phẩm)
    /// ============================================================================
    /// </summary>
    public class ProductCategoryServiceTests
    {
        private readonly IMapper _mapper;

        public ProductCategoryServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM THEO TỪ KHÓA / ĐA NHÓM NGÀNH HÀNG / NGÀY
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo từ khóa, lọc theo chuỗi phân tách dấu phẩy categoryGroupId ("1, 2, abc"), ngày tạo/cập nhật.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.AddRange(
                new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" },
                new ProductCategoryGroup { Id = 2, Code = "BEV", Name = "Đồ uống" }
            );

            context.ProductCategories.AddRange(
                new ProductCategory { Id = 1, Code = "VEG", Name = "Rau xanh hữu cơ", CategoryGroupId = 1, IsActive = true, CreatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc) },
                new ProductCategory { Id = 2, Code = "FRUIT", Name = "Trái cây nhiệt đới", CategoryGroupId = 1, IsActive = true, CreatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc) },
                new ProductCategory { Id = 3, Code = "JUICE", Name = "Nước ép trái cây", CategoryGroupId = 2, IsActive = false, CreatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc) }
            );
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "trái cây"
            var searchResult = await service.GetPagedAsync("trái cây", null, null, null, null, null, 1, 10);

            // Act 2: Lọc theo danh sách CategoryGroupId "1, 2, invalid"
            var groupFilterResult = await service.GetPagedAsync(null, null, "1, invalid", null, null, null, 1, 10);

            // Act 3: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, null, false, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(2); // "Trái cây nhiệt đới" and "Nước ép trái cây"
            groupFilterResult.TotalRecords.Should().Be(2); // ID 1 and 2
            statusResult.TotalRecords.Should().Be(1); // ID 3
        }
        #endregion

        #region TC02: LẤY TOÀN BỘ DANH SÁCH DANH MỤC
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách danh mục (kèm CategoryGroupName) cho dropdown.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnCategoriesWithGroupMapping()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm tươi sống" });
            context.ProductCategories.AddRange(
                new ProductCategory { Id = 1, Code = "VEG", Name = "Rau xanh", CategoryGroupId = 1, IsActive = true },
                new ProductCategory { Id = 2, Code = "MEAT", Name = "Thịt tươi", CategoryGroupId = 1, IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act
            var all = await service.GetAllListAsync(false);
            var activeOnly = await service.GetAllListAsync(true);

            // Assert
            all.Should().HaveCount(2);
            all.First().CategoryGroupName.Should().Be("Thực phẩm tươi sống");

            activeOnly.Should().HaveCount(1);
            activeOnly.First().Code.Should().Be("VEG");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết danh mục theo ID hợp lệ và trả về null khi không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCategoryWithGroupDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" });
            context.ProductCategories.Add(new ProductCategory { Id = 10, Code = "VEG", Name = "Rau củ", CategoryGroupId = 1, Description = "Nông sản sạch" });
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(10);
            var invalid = await service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid!.Code.Should().Be("VEG");
            valid.Name.Should().Be("Rau củ");
            valid.CategoryGroupName.Should().Be("Thực phẩm");

            invalid.Should().BeNull();
        }
        #endregion

        #region TC04: TẠO MỚI DANH MỤC THÀNH CÔNG (CHUẨN HÓA MÃ CODE)
        /// <summary>
        /// TC04: Tạo mới danh mục sản phẩm thành công, tự động viết hoa Code và cắt tỉa khoảng trắng.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" });
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            var dto = new ProductCategoryCreateDto
            {
                Code = "  organic_veg  ",
                Name = "  Rau Hữu Cơ  ",
                Description = "  Trồng theo chuẩn VietGAP  ",
                CategoryGroupId = 1,
                ImagePath = "  /images/veg.png  ",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.ProductCategories.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("ORGANIC_VEG");
            createdEntity.Name.Should().Be("Rau Hữu Cơ");
            createdEntity.CategoryGroupId.Should().Be(1);
            createdEntity.ImagePath.Should().Be("/images/veg.png");
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC05: CHẶN TRÙNG MÃ DANH MỤC & NHÓM NGÀNH HÀNG KHÔNG TỒN TẠI
        /// <summary>
        /// TC05: Ném InvalidOperationException khi mã danh mục bị trùng lặp hoặc CategoryGroupId không tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCode_OrInvalidGroup()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "VEG", Name = "Rau xanh" });
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act & Assert 1: Trùng mã
            var duplicateCodeDto = new ProductCategoryCreateDto { Code = "veg", Name = "Rau củ khác" };
            var actDuplicate = () => service.CreateAsync(duplicateCodeDto);
            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã danh mục 'veg' đã tồn tại*");

            // Act & Assert 2: Nhóm ngành hàng không tồn tại
            var invalidGroupDto = new ProductCategoryCreateDto { Code = "NEW_CAT", Name = "Danh mục mới", CategoryGroupId = 999 };
            var actInvalidGroup = () => service.CreateAsync(invalidGroupDto);
            await actInvalidGroup.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Nhóm ngành hàng được chọn không tồn tại.*");
        }
        #endregion

        #region TC06: CẬP NHẬT THÀNH CÔNG VÀ KIỂM TRA RÀNG BUỘC
        /// <summary>
        /// TC06: Cập nhật danh mục thành công, chặn nếu mã bị trùng lặp với danh mục khác hoặc CategoryGroupId không hợp lệ.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndValidateUniquenessAndGroup()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" });
            context.ProductCategories.AddRange(
                new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ", CategoryGroupId = 1 },
                new ProductCategory { Id = 2, Code = "FRUIT", Name = "Trái cây", CategoryGroupId = 1 }
            );
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new ProductCategoryUpdateDto { Code = "VEG", Name = "Rau củ quả Đà Lạt", CategoryGroupId = 1, IsActive = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với bản ghi ID = 2 (FRUIT)
            var duplicateDto = new ProductCategoryUpdateDto { Code = "fruit", Name = "Tên bất kỳ" };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Act 3: Cập nhật với CategoryGroupId không tồn tại
            var invalidGroupDto = new ProductCategoryUpdateDto { Code = "VEG", Name = "Rau củ", CategoryGroupId = 999 };
            var actInvalidGroup = () => service.UpdateAsync(1, invalidGroupDto);

            // Act 4: Cập nhật bản ghi không tồn tại
            var notFoundDto = new ProductCategoryUpdateDto { Code = "NONE", Name = "None" };
            var actNotFound = () => service.UpdateAsync(999, notFoundDto);

            // Assert
            updateResult.Should().BeTrue();
            var updated = await context.ProductCategories.FindAsync(1);
            updated!.Name.Should().Be("Rau củ quả Đà Lạt");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã danh mục 'fruit' đã bị trùng lặp*");

            await actInvalidGroup.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Nhóm ngành hàng được chọn không tồn tại.*");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy danh mục sản phẩm*");
        }
        #endregion

        #region TC07: SAFETY SHIELD - CHẶN XÓA KHI CÓ SẢN PHẨM TRỰC THUỘC
        /// <summary>
        /// TC07: Đảm bảo tính toàn vẹn dữ liệu: Chặn xóa danh mục nếu đang có Sản phẩm (Product) liên kết.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasProducts()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ" });
            context.Products.Add(new Product
            {
                Id = 100,
                Code = "SP01",
                Name = "Cà chua bi Đà Lạt",
                Slug = "ca-chua-bi-da-lat",
                CategoryId = 1,
                BaseUoMId = 1,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa danh mục này vì đang có sản phẩm thuộc danh mục.*");
        }
        #endregion

        #region TC08: XÓA THÀNH CÔNG VÀ CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG
        /// <summary>
        /// TC08: Xóa thành công khi không có ràng buộc (Soft Delete) và Toggle Active trạng thái.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_And_ToggleActiveAsync_ShouldWorkCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "TEST", Name = "Danh mục thử nghiệm", IsActive = true });
            await context.SaveChangesAsync();

            var service = new ProductCategoryService(context, _mapper);

            // Act 1: Toggle Active
            await service.ToggleActiveAsync(1);
            var entityAfterToggle = await context.ProductCategories.FindAsync(1);
            entityAfterToggle!.IsActive.Should().BeFalse();

            // Act 2: Xóa thành công
            var deleteResult = await service.DeleteAsync(1);

            // Assert
            deleteResult.Should().BeTrue();
            var deletedEntity = await context.ProductCategories.FindAsync(1);
            deletedEntity!.IsDeleted.Should().BeTrue();
            deletedEntity.DeletedAt.Should().NotBeNull();
        }
        #endregion
    }
}
