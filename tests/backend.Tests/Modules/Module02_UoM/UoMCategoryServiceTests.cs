using AutoMapper;
using backend.Data;
using backend.DTOs.UoMCategoryDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module02_UoM
{
    /// <summary>
    /// ============================================================================
    /// MODULE 2: UNIT OF MEASURE (UoM) MASTER DATA
    /// UNIT TEST: UoMCategoryService (Quản lý Nhóm Đơn vị tính)
    /// ============================================================================
    /// </summary>
    public class UoMCategoryServiceTests
    {
        private readonly IMapper _mapper;

        public UoMCategoryServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM THEO TỪ KHÓA
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo từ khóa (Code, Name) và phân trang.
        /// Kịch bản:
        /// - Tạo 3 nhóm: WEIGHT (Khối lượng), VOLUME (Thể tích), PACK (Đóng gói - IsActive=false).
        /// - Tìm kiếm từ khóa "tích" -> Phải trả về đúng nhóm VOLUME.
        /// - Lọc theo IsActive = false -> Phải trả về nhóm PACK.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.AddRange(
                new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng", IsActive = true },
                new UoMCategory { Id = 2, Code = "VOLUME", Name = "Thể tích", IsActive = true },
                new UoMCategory { Id = 3, Code = "PACK", Name = "Đóng gói", IsActive = false }
            );
            await context.SaveChangesAsync();

            var service = new UoMCategoryService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "tích"
            var searchResult = await service.GetPagedAsync("tích", null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái IsActive = false
            var statusResult = await service.GetPagedAsync(null, false, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("VOLUME");

            statusResult.TotalRecords.Should().Be(1);
            statusResult.Items.First().Code.Should().Be("PACK");
        }
        #endregion

        #region TC02: LẤY CHI TIẾT THEO ID KÈM BASE UOM
        /// <summary>
        /// TC02: Lấy chi tiết nhóm ĐVT theo ID, kiểm tra AutoMapper có ánh xạ đúng BaseUoMCode và BaseUoMName.
        /// Kịch bản:
        /// - Tạo nhóm WEIGHT có BaseUoM là Kilogram (KG).
        /// - Gọi GetByIdAsync(1) -> DTO phải có BaseUoMCode="KG" và BaseUoMName="Kilogram".
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldIncludeBaseUoMInfo()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var baseUoM = new UoM { Id = 10, Code = "KG", Name = "Kilogram", IsActive = true };
            var category = new UoMCategory
            {
                Id = 1,
                Code = "WEIGHT",
                Name = "Khối lượng",
                BaseUoMId = 10,
                BaseUoM = baseUoM
            };

            context.UoMs.Add(baseUoM);
            context.UoMCategories.Add(category);
            await context.SaveChangesAsync();

            var service = new UoMCategoryService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Code.Should().Be("WEIGHT");
            result.BaseUoMId.Should().Be(10);
            result.BaseUoMCode.Should().Be("KG");
            result.BaseUoMName.Should().Be("Kilogram");
        }
        #endregion

        #region TC03: CHẶN TRÙNG MÃ CODE (CASE-INSENSITIVE)
        /// <summary>
        /// TC03: Hệ thống phải ném InvalidOperationException khi tạo nhóm có mã Code đã tồn tại (không phân biệt hoa/thường).
        /// Kịch bản:
        /// - Đã có nhóm "WEIGHT".
        /// - Tạo nhóm mới với Code = "  weight  " -> Phải bị chặn.
        /// </summary>
        [Fact]
        public async Task CreateAsync_DuplicateCode_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            await context.SaveChangesAsync();

            var service = new UoMCategoryService(context, _mapper);
            var dto = new UoMCategoryCreateDto
            {
                Code = "  weight  ",
                Name = "Cân nặng mới"
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại*");
        }
        #endregion

        #region TC04: CHẶN TRÙNG TÊN NHÓM NAME
        /// <summary>
        /// TC04: Hệ thống phải ném InvalidOperationException khi tạo nhóm có tên Name đã tồn tại (không phân biệt hoa/thường).
        /// Kịch bản:
        /// - Đã có nhóm Name = "Khối lượng".
        /// - Tạo nhóm mới với Name = "  khối lượng  " -> Phải bị chặn.
        /// </summary>
        [Fact]
        public async Task CreateAsync_DuplicateName_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            await context.SaveChangesAsync();

            var service = new UoMCategoryService(context, _mapper);
            var dto = new UoMCategoryCreateDto
            {
                Code = "MASS",
                Name = "  khối lượng  "
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại*");
        }
        #endregion

        #region TC05: TẠO MỚI NHÓM ĐVT THÀNH CÔNG
        /// <summary>
        /// TC05: Tạo mới nhóm ĐVT thành công, tự động chuẩn hóa viết hoa mã Code và lưu vào CSDL.
        /// Kịch bản:
        /// - Gửi dto Code = "volume", Name = "Thể tích".
        /// - Kiểm tra ID trả về > 0, Entity trong DB có Code = "VOLUME", CreatedAt được gán.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ValidDto_ShouldPersistAndReturnId()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new UoMCategoryService(context, _mapper);
            var dto = new UoMCategoryCreateDto
            {
                Code = "volume",
                Name = "Thể tích",
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var savedEntity = await context.UoMCategories.FindAsync(newId);
            savedEntity.Should().NotBeNull();
            savedEntity!.Code.Should().Be("VOLUME");
            savedEntity.Name.Should().Be("Thể tích");
            savedEntity.IsActive.Should().BeTrue();
            savedEntity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
        #endregion

        #region TC06: CHẶN XÓA NHÓM KHI CÓ ĐVT CON TRỰC THUỘC (SAFETY SHIELD)
        /// <summary>
        /// TC06: Ngăn chặn xóa nhóm ĐVT nếu đang có đơn vị tính con thuộc nhóm này.
        /// Kịch bản:
        /// - Nhóm WEIGHT (Id=1) đang chứa đơn vị tính KG (CategoryId=1).
        /// - Gọi DeleteAsync(1) -> Phải ném InvalidOperationException với cảnh báo còn ĐVT trực thuộc.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenHasChildUoMs_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            context.UoMs.Add(new UoM { Id = 10, Code = "KG", Name = "Kilogram", CategoryId = 1, IsDeleted = false });
            await context.SaveChangesAsync();

            var service = new UoMCategoryService(context, _mapper);

            // Act
            var act = async () => await service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đang có các Đơn vị tính trực thuộc*");
        }
        #endregion

        #region TC07: ĐẢO TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC07: Đảo ngược trạng thái hoạt động của nhóm ĐVT giữa Kích hoạt và Tạm khóa.
        /// Kịch bản:
        /// - Nhóm ban đầu có IsActive = true.
        /// - Gọi ToggleActiveAsync(1) -> IsActive chuyển thành false và UpdatedAt được cập nhật.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ExistingId_ShouldInvertStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng", IsActive = true });
            await context.SaveChangesAsync();

            var service = new UoMCategoryService(context, _mapper);

            // Act
            var newStatus = await service.ToggleActiveAsync(1);

            // Assert
            newStatus.Should().BeFalse();

            var updatedEntity = await context.UoMCategories.FindAsync(1);
            updatedEntity!.IsActive.Should().BeFalse();
            updatedEntity.UpdatedAt.Should().NotBeNull();
        }
        #endregion
    }
}
