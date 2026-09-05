using AutoMapper;
using backend.DTOs.CategoryAttributeDTOs;
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
    /// UNIT TEST: CategoryAttributeService (Cấu Hình Mẫu Thuộc Tính Danh Mục - EAV Template)
    /// ============================================================================
    /// </summary>
    public class CategoryAttributeServiceTests
    {
        private readonly IMapper _mapper;

        public CategoryAttributeServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM THEO TÊN / DANH MỤC / THUỘC TÍNH
        /// <summary>
        /// TC01: Kiểm tra tìm kiếm theo tên danh mục/thuộc tính, lọc theo danh sách categoryId và attributeDefinitionId.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.AddRange(
                new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ sạch" },
                new ProductCategory { Id = 2, Code = "FRUIT", Name = "Trái cây nhập khẩu" }
            );

            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 10, Name = "Độ ngọt (Brix)", DataType = "NUMBER" },
                new AttributeDefinition { Id = 11, Name = "Xuất xứ", DataType = "STRING" }
            );

            context.CategoryAttributes.AddRange(
                new CategoryAttribute { Id = 1, CategoryId = 1, AttributeDefinitionId = 11, IsRequired = false },
                new CategoryAttribute { Id = 2, CategoryId = 2, AttributeDefinitionId = 10, IsRequired = true },
                new CategoryAttribute { Id = 3, CategoryId = 2, AttributeDefinitionId = 11, IsRequired = true }
            );
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "ngọt"
            var searchResult = await service.GetPagedAsync("ngọt", null, null, 1, 10);

            // Act 2: Lọc theo CategoryId = "2"
            var categoryFilterResult = await service.GetPagedAsync(null, "2, invalid", null, 1, 10);

            // Act 3: Lọc theo AttributeDefinitionId = "11"
            var attributeFilterResult = await service.GetPagedAsync(null, null, "11", 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().AttributeDefinitionName.Should().Be("Độ ngọt (Brix)");

            categoryFilterResult.TotalRecords.Should().Be(2); // CategoryId 2 has 2 attributes

            attributeFilterResult.TotalRecords.Should().Be(2); // AttributeDefinitionId 11 used in Category 1 and 2
        }
        #endregion

        #region TC02: LẤY TOÀN BỘ DANH SÁCH KÈM TÊN ÁNH XẠ
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách cấu hình kèm CategoryName và AttributeDefinitionName.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnCategoryAttributesWithEnrichedNames()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "FRUIT", Name = "Trái cây" });
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 10, Name = "Độ ngọt", DataType = "NUMBER" });
            context.CategoryAttributes.Add(new CategoryAttribute { Id = 1, CategoryId = 1, AttributeDefinitionId = 10, IsRequired = true });
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            // Act
            var all = await service.GetAllListAsync();

            // Assert
            all.Should().HaveCount(1);
            var item = all.First();
            item.CategoryName.Should().Be("Trái cây");
            item.AttributeDefinitionName.Should().Be("Độ ngọt");
            item.IsRequired.Should().BeTrue();
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết cấu hình theo ID hợp lệ và ném KeyNotFoundException nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnData_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "FRUIT", Name = "Trái cây" });
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 10, Name = "Độ ngọt", DataType = "NUMBER" });
            context.CategoryAttributes.Add(new CategoryAttribute { Id = 5, CategoryId = 1, AttributeDefinitionId = 10, IsRequired = true });
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(5);
            var actInvalid = () => service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid.CategoryId.Should().Be(1);
            valid.AttributeDefinitionId.Should().Be(10);
            valid.IsRequired.Should().BeTrue();

            await actInvalid.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy cấu hình thuộc tính danh mục.*");
        }
        #endregion

        #region TC04: GÁN MỚI THUỘC TÍNH VÀO DANH MỤC THÀNH CÔNG
        /// <summary>
        /// TC04: Thiết lập gán mới thuộc tính từ từ điển vào danh mục sản phẩm thành công.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WhenValidData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ" });
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 10, Name = "Xuất xứ", DataType = "STRING" });
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            var dto = new CategoryAttributeCreateDto
            {
                CategoryId = 1,
                AttributeDefinitionId = 10,
                IsRequired = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.CategoryAttributes.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.CategoryId.Should().Be(1);
            createdEntity.AttributeDefinitionId.Should().Be(10);
            createdEntity.IsRequired.Should().BeTrue();
        }
        #endregion

        #region TC05: BÁO LỖI KHI DANH MỤC HOẶC THUỘC TÍNH KHÔNG TỒN TẠI
        /// <summary>
        /// TC05: Ném InvalidOperationException khi CategoryId hoặc AttributeDefinitionId không tồn tại trên hệ thống.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenCategoryOrAttributeNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ" });
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 10, Name = "Xuất xứ", DataType = "STRING" });
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            // Act & Assert 1: Category không tồn tại
            var invalidCategoryDto = new CategoryAttributeCreateDto { CategoryId = 999, AttributeDefinitionId = 10 };
            var actCategory = () => service.CreateAsync(invalidCategoryDto);
            await actCategory.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Danh mục sản phẩm được chỉ định không tồn tại.*");

            // Act & Assert 2: Attribute không tồn tại
            var invalidAttributeDto = new CategoryAttributeCreateDto { CategoryId = 1, AttributeDefinitionId = 999 };
            var actAttribute = () => service.CreateAsync(invalidAttributeDto);
            await actAttribute.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Định nghĩa thuộc tính từ điển không tồn tại.*");
        }
        #endregion

        #region TC06: ĐẶC THÙ - CHẶN GÁN TRÙNG 1 THUỘC TÍNH 2 LẦN CHO CÙNG 1 DANH MỤC
        /// <summary>
        /// TC06: Đảm bảo tính toàn vẹn mẫu EAV: Chặn gán trùng lặp cùng một AttributeDefinitionId cho cùng một CategoryId.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCategoryAttributePair()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ" });
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 10, Name = "Xuất xứ", DataType = "STRING" });
            context.CategoryAttributes.Add(new CategoryAttribute { Id = 1, CategoryId = 1, AttributeDefinitionId = 10 });
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            var duplicateDto = new CategoryAttributeCreateDto
            {
                CategoryId = 1,
                AttributeDefinitionId = 10,
                IsRequired = false
            };

            // Act
            var act = () => service.CreateAsync(duplicateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Thuộc tính này đã được thiết lập cho danh mục được chọn.*");
        }
        #endregion

        #region TC07: CẬP NHẬT CẤU HÌNH VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC07: Cập nhật cấu hình thuộc tính thành công, chặn nếu đổi sang cặp (CategoryId, AttributeDefinitionId) đã tồn tại.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicateAssignment()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "VEG", Name = "Rau củ" });
            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 10, Name = "Xuất xứ", DataType = "STRING" },
                new AttributeDefinition { Id = 11, Name = "Độ ngọt", DataType = "NUMBER" }
            );
            context.CategoryAttributes.AddRange(
                new CategoryAttribute { Id = 1, CategoryId = 1, AttributeDefinitionId = 10, IsRequired = false },
                new CategoryAttribute { Id = 2, CategoryId = 1, AttributeDefinitionId = 11, IsRequired = true }
            );
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            // Act 1: Cập nhật hợp lệ (thay đổi IsRequired)
            var updateDto = new CategoryAttributeUpdateDto { CategoryId = 1, AttributeDefinitionId = 10, IsRequired = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật bản ghi ID = 1 đổi sang AttributeDefinitionId = 11 (trùng với bản ghi ID = 2)
            var duplicateDto = new CategoryAttributeUpdateDto { CategoryId = 1, AttributeDefinitionId = 11 };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Act 3: Cập nhật bản ghi không tồn tại
            var notFoundDto = new CategoryAttributeUpdateDto { CategoryId = 1, AttributeDefinitionId = 10 };
            var actNotFound = () => service.UpdateAsync(999, notFoundDto);

            // Assert
            updateResult.Should().BeTrue();
            var updated = await context.CategoryAttributes.FindAsync(1);
            updated!.IsRequired.Should().BeTrue();

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cập nhật thất bại: Thuộc tính này đã tồn tại trong danh mục.*");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy cấu hình thuộc tính cần cập nhật.*");
        }
        #endregion

        #region TC08: XÓA CẤU HÌNH THUỘC TÍNH KHỎI DANH MỤC
        /// <summary>
        /// TC08: Hủy gán thuộc tính khỏi danh mục thành công và ném KeyNotFoundException nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldDeleteSuccessfully_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.CategoryAttributes.Add(new CategoryAttribute { Id = 1, CategoryId = 1, AttributeDefinitionId = 10 });
            await context.SaveChangesAsync();

            var service = new CategoryAttributeService(context, _mapper);

            // Act 1: Xóa bản ghi hợp lệ
            var deleteResult = await service.DeleteAsync(1);

            // Act 2: Xóa bản ghi không tồn tại
            var actNotFound = () => service.DeleteAsync(999);

            // Assert
            deleteResult.Should().BeTrue();
            var remainingCount = await context.CategoryAttributes.CountAsync();
            remainingCount.Should().Be(0);

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy cấu hình thuộc tính để xóa.*");
        }
        #endregion
    }
}
