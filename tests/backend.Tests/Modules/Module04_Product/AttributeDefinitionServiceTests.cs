using AutoMapper;
using backend.DTOs.AttributeDefinitionDTOs;
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
    /// 📦 MODULE 4: PRODUCT MASTER DATA
    /// 🧪 UNIT TEST: AttributeDefinitionService (Từ Điển Thuộc Tính Động - EAV)
    /// ============================================================================
    /// </summary>
    public class AttributeDefinitionServiceTests
    {
        private readonly IMapper _mapper;

        public AttributeDefinitionServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM THEO TÊN / KIỂU DỮ LIỆU / TRẠNG THÁI
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo tên thuộc tính, lọc theo DataType (STRING, NUMBER, BOOLEAN...) và trạng thái.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 1, Name = "Độ ngọt (Brix)", DataType = "NUMBER", IsActive = true, CreatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc) },
                new AttributeDefinition { Id = 2, Name = "Xuất xứ", DataType = "STRING", IsActive = true, CreatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc) },
                new AttributeDefinition { Id = 3, Name = "Hữu cơ (Organic)", DataType = "BOOLEAN", IsActive = false, CreatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc) }
            );
            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            // Act 1: Tìm kiếm theo tên "ngọt"
            var searchResult = await service.GetPagedAsync("ngọt", null, null, null, null, 1, 10);

            // Act 2: Lọc theo DataType = "string"
            var typeResult = await service.GetPagedAsync(null, "string", null, null, null, 1, 10);

            // Act 3: Lọc theo IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Name.Should().Be("Độ ngọt (Brix)");

            typeResult.TotalRecords.Should().Be(1);
            typeResult.Items.First().Name.Should().Be("Xuất xứ");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Name.Should().Be("Hữu cơ (Organic)");
        }
        #endregion

        #region TC02: LẤY TOÀN BỘ DANH SÁCH TỪ ĐIỂN
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách định nghĩa thuộc tính phục vụ Dropdown.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllOrActiveOnly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 1, Name = "Độ ngọt", DataType = "NUMBER", IsActive = true },
                new AttributeDefinition { Id = 2, Name = "Màu sắc", DataType = "STRING", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            // Act
            var all = await service.GetAllListAsync(false);
            var activeOnly = await service.GetAllListAsync(true);

            // Assert
            all.Should().HaveCount(2);
            activeOnly.Should().HaveCount(1);
            activeOnly.First().Name.Should().Be("Độ ngọt");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết thuộc tính theo ID hợp lệ và ném KeyNotFoundException nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 1, Name = "Độ ngọt", DataType = "NUMBER" });
            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(1);
            var actInvalid = () => service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid.Name.Should().Be("Độ ngọt");
            valid.DataType.Should().Be("NUMBER");

            await actInvalid.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy từ điển thuộc tính.*");
        }
        #endregion

        #region TC04: TẠO MỚI THÀNH CÔNG VỚI DỮ LIỆU ĐƯỢC CHUẨN HÓA
        /// <summary>
        /// TC04: Tạo mới thuộc tính thành công, tự động chuẩn hóa viết hoa DataType và cắt tỉa tên.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new AttributeDefinitionService(context, _mapper);

            var dto = new AttributeDefinitionCreateDto
            {
                Name = "  Quy cách đóng gói  ",
                DataType = "  string  ",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.AttributeDefinitions.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Name.Should().Be("Quy cách đóng gói");
            createdEntity.DataType.Should().Be("STRING");
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC05: CHẶN TRÙNG TÊN THUỘC TÍNH (CASE-INSENSITIVE)
        /// <summary>
        /// TC05: Ném InvalidOperationException khi tạo thuộc tính có tên đã tồn tại trong từ điển.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateName()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 1, Name = "Độ ngọt", DataType = "NUMBER" });
            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            var duplicateDto = new AttributeDefinitionCreateDto
            {
                Name = "  độ ngọt  ", // Viết thường và có khoảng trắng
                DataType = "NUMBER"
            };

            // Act
            var act = () => service.CreateAsync(duplicateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Thuộc tính có tên 'độ ngọt' đã tồn tại trong từ điển hệ thống.*");
        }
        #endregion

        #region TC06: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC06: Cập nhật thông tin thuộc tính thành công, chặn nếu tên bị trùng với thuộc tính khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicateName()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 1, Name = "Độ ngọt", DataType = "NUMBER" },
                new AttributeDefinition { Id = 2, Name = "Xuất xứ", DataType = "STRING" }
            );
            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            // Act 1: Cập nhật hợp lệ
            var updateDto = new AttributeDefinitionUpdateDto { Name = "Độ ngọt tiêu chuẩn (Brix)", DataType = "NUMBER", IsActive = true };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng tên với bản ghi ID = 2 (Xuất xứ)
            var duplicateDto = new AttributeDefinitionUpdateDto { Name = "xuất xứ", DataType = "STRING" };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Act 3: Cập nhật bản ghi không tồn tại
            var notFoundDto = new AttributeDefinitionUpdateDto { Name = "None", DataType = "STRING" };
            var actNotFound = () => service.UpdateAsync(999, notFoundDto);

            // Assert
            updateResult.Should().BeTrue();
            var updated = await context.AttributeDefinitions.FindAsync(1);
            updated!.Name.Should().Be("Độ ngọt tiêu chuẩn (Brix)");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Tên thuộc tính 'xuất xứ' đã bị trùng lặp*");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy từ điển thuộc tính*");
        }
        #endregion

        #region TC07: SAFETY SHIELDS - CHẶN XÓA KHI ĐANG ĐƯỢC GÁN VÀO DANH MỤC HOẶC BIẾN THỂ
        /// <summary>
        /// TC07: Đảm bảo tính toàn vẹn EAV: Chặn xóa thuộc tính nếu đang được cấu hình cho Danh mục hoặc gán giá trị cho Biến thể.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenUsedInCategories_OrVariants()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.AttributeDefinitions.AddRange(
                new AttributeDefinition { Id = 1, Name = "Độ ngọt", DataType = "NUMBER" },
                new AttributeDefinition { Id = 2, Name = "Xuất xứ", DataType = "STRING" }
            );

            // Gán thuộc tính 1 vào Danh mục
            context.CategoryAttributes.Add(new CategoryAttribute { Id = 10, CategoryId = 1, AttributeDefinitionId = 1 });

            // Gán thuộc tính 2 vào Biến thể sản phẩm
            context.ProductAttributes.Add(new ProductAttribute { Id = 20, VariantId = 1, AttributeDefinitionId = 2, AttributeValue = "Đà Lạt", IsDeleted = false });

            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            // Act 1: Xóa thuộc tính đang gán cho Danh mục
            var actCategory = () => service.DeleteAsync(1);

            // Act 2: Xóa thuộc tính đang gán cho Biến thể sản phẩm
            var actVariant = () => service.DeleteAsync(2);

            // Assert
            await actCategory.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa thuộc tính này vì đang được gán vào mẫu thuộc tính danh mục sản phẩm.*");

            await actVariant.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa thuộc tính này vì đang được sử dụng trong các biến thể sản phẩm.*");
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
            context.AttributeDefinitions.Add(new AttributeDefinition { Id = 1, Name = "Thử nghiệm", DataType = "STRING", IsActive = true });
            await context.SaveChangesAsync();

            var service = new AttributeDefinitionService(context, _mapper);

            // Act 1: Toggle Active
            await service.ToggleActiveAsync(1);
            var entityAfterToggle = await context.AttributeDefinitions.FindAsync(1);
            entityAfterToggle!.IsActive.Should().BeFalse();

            // Act 2: Xóa thành công
            var deleteResult = await service.DeleteAsync(1);

            // Assert
            deleteResult.Should().BeTrue();
            var deletedEntity = await context.AttributeDefinitions.FindAsync(1);
            deletedEntity!.IsDeleted.Should().BeTrue();
            deletedEntity.DeletedAt.Should().NotBeNull();
        }
        #endregion
    }
}
