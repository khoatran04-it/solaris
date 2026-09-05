using AutoMapper;
using backend.DTOs.ProductCategoryGroupDTOs;
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
    /// UNIT TEST: ProductCategoryGroupService (Quản lý Nhóm Ngành Hàng Lớn)
    /// ============================================================================
    /// </summary>
    public class ProductCategoryGroupServiceTests
    {
        private readonly IMapper _mapper;

        public ProductCategoryGroupServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM THEO TỪ KHÓA / TÊN / TRẠNG THÁI / NGÀY
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo từ khóa (Code, Name), lọc theo danh sách tên, trạng thái và ngày tạo/cập nhật.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.AddRange(
                new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm tươi sống", IsActive = true, CreatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc) },
                new ProductCategoryGroup { Id = 2, Code = "BEV", Name = "Đồ uống & Giải khát", IsActive = true, CreatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc) },
                new ProductCategoryGroup { Id = 3, Code = "DRY", Name = "Đồ khô & Đóng hộp", IsActive = false, CreatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc) }
            );
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "thực phẩm"
            var searchResult = await service.GetPagedAsync("thực phẩm", null, null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, false, null, null, 1, 10);

            // Act 3: Lọc theo danh sách tên "Đồ uống & Giải khát"
            var nameResult = await service.GetPagedAsync(null, "Đồ uống & Giải khát", null, null, null, 1, 10);

            // Act 4: Lọc theo ngày tạo
            var dateResult = await service.GetPagedAsync(null, null, null, new DateTime(2026, 8, 21), null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("FOOD");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("DRY");

            nameResult.TotalRecords.Should().Be(1);
            nameResult.Items.First().Code.Should().Be("BEV");

            dateResult.TotalRecords.Should().Be(1);
            dateResult.Items.First().Code.Should().Be("BEV");
        }
        #endregion

        #region TC02: LẤY TOÀN BỘ DANH SÁCH RÚT GỌN (CHO DROPDOWN)
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách nhóm ngành hàng và hỗ trợ cờ lọc isActiveOnly.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllOrActiveOnly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.AddRange(
                new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm", IsActive = true },
                new ProductCategoryGroup { Id = 2, Code = "BEV", Name = "Đồ uống", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            // Act
            var all = await service.GetAllListAsync(false);
            var activeOnly = await service.GetAllListAsync(true);

            // Assert
            all.Should().HaveCount(2);
            activeOnly.Should().HaveCount(1);
            activeOnly.First().Code.Should().Be("FOOD");
        }
        #endregion

        #region TC03: LẤY CHI TIẾT THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết nhóm ngành hàng theo ID hợp lệ và trả về null khi không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCorrectData_OrNull()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm tươi sống", Description = "Rau củ quả sạch" });
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(1);
            var invalid = await service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid!.Code.Should().Be("FOOD");
            valid.Name.Should().Be("Thực phẩm tươi sống");
            valid.Description.Should().Be("Rau củ quả sạch");

            invalid.Should().BeNull();
        }
        #endregion

        #region TC04: TẠO MỚI THÀNH CÔNG VỚI DỮ LIỆU ĐƯỢC CHUẨN HÓA
        /// <summary>
        /// TC04: Tạo mới nhóm ngành hàng thành công, tự động viết hoa Code và cắt tỉa khoảng trắng.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateSuccessfully_WithNormalizedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new ProductCategoryGroupService(context, _mapper);

            var dto = new ProductCategoryGroupCreateDto
            {
                Code = "  fresh_fruit  ",
                Name = "  Trái Cây Tươi  ",
                Description = "  Trái cây nhập khẩu và nội địa  ",
                ImagePath = "  /images/fruits.png  ",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.ProductCategoryGroups.FindAsync(newId);
            createdEntity.Should().NotBeNull();
            createdEntity!.Code.Should().Be("FRESH_FRUIT");
            createdEntity.Name.Should().Be("Trái Cây Tươi");
            createdEntity.Description.Should().Be("Trái cây nhập khẩu và nội địa");
            createdEntity.ImagePath.Should().Be("/images/fruits.png");
            createdEntity.IsActive.Should().BeTrue();
        }
        #endregion

        #region TC05: CHẶN TRÙNG MÃ NHÓM NGÀNH HÀNG (CASE-INSENSITIVE)
        /// <summary>
        /// TC05: Ném InvalidOperationException khi tạo mới với mã nhóm đã tồn tại (không phân biệt hoa thường).
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" });
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            var duplicateDto = new ProductCategoryGroupCreateDto
            {
                Code = "food", // Viết thường trùng với FOOD
                Name = "Thực phẩm mới"
            };

            // Act
            var act = () => service.CreateAsync(duplicateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã nhóm ngành hàng 'food' đã tồn tại*");
        }
        #endregion

        #region TC06: CẬP NHẬT THÀNH CÔNG VÀ CHẶN TRÙNG LẶP
        /// <summary>
        /// TC06: Cập nhật thông tin thành công, cho phép giữ nguyên mã của chính mình, chặn nếu trùng mã với bản ghi khác.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndPreventDuplicateCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.AddRange(
                new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" },
                new ProductCategoryGroup { Id = 2, Code = "BEV", Name = "Đồ uống" }
            );
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            // Act 1: Cập nhật hợp lệ (giữ nguyên mã FOOD hoặc đổi tên)
            var updateDto = new ProductCategoryGroupUpdateDto
            {
                Code = "FOOD",
                Name = "Thực phẩm sạch cao cấp",
                Description = "Nông sản hữu cơ",
                IsActive = true
            };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật trùng mã với bản ghi ID = 2 (BEV)
            var duplicateDto = new ProductCategoryGroupUpdateDto
            {
                Code = "bev",
                Name = "Tên bất kỳ",
                IsActive = true
            };
            var actDuplicate = () => service.UpdateAsync(1, duplicateDto);

            // Act 3: Cập nhật bản ghi không tồn tại
            var notFoundDto = new ProductCategoryGroupUpdateDto { Code = "NONE", Name = "None" };
            var actNotFound = () => service.UpdateAsync(999, notFoundDto);

            // Assert
            updateResult.Should().BeTrue();
            var updated = await context.ProductCategoryGroups.FindAsync(1);
            updated!.Name.Should().Be("Thực phẩm sạch cao cấp");

            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã nhóm ngành hàng 'bev' đã bị trùng lặp*");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy nhóm ngành hàng*");
        }
        #endregion

        #region TC07: SAFETY SHIELD - CHẶN XÓA KHI CÓ DANH MỤC TRỰC THUỘC
        /// <summary>
        /// TC07: Đảm bảo tính toàn vẹn dữ liệu: Chặn xóa nhóm ngành hàng nếu đang chứa các danh mục hàng hóa con.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasChildCategories()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm" });
            context.ProductCategories.Add(new ProductCategory
            {
                Id = 10,
                Code = "VEG",
                Name = "Rau củ quả",
                CategoryGroupId = 1,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            // Act
            var act = () => service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa nhóm ngành hàng này vì đang chứa các danh mục hàng hóa trực thuộc.*");
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
            context.ProductCategoryGroups.Add(new ProductCategoryGroup { Id = 1, Code = "TEST", Name = "Nhóm thử nghiệm", IsActive = true });
            await context.SaveChangesAsync();

            var service = new ProductCategoryGroupService(context, _mapper);

            // Act 1: Toggle Active
            await service.ToggleActiveAsync(1);
            var entityAfterToggle = await context.ProductCategoryGroups.FindAsync(1);
            entityAfterToggle!.IsActive.Should().BeFalse();

            // Act 2: Xóa thành công
            var deleteResult = await service.DeleteAsync(1);

            // Assert
            deleteResult.Should().BeTrue();
            var deletedEntity = await context.ProductCategoryGroups.FindAsync(1);
            deletedEntity!.IsDeleted.Should().BeTrue();
            deletedEntity.DeletedAt.Should().NotBeNull();
        }
        #endregion
    }
}
