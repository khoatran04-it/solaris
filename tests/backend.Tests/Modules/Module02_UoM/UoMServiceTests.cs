using AutoMapper;
using backend.Data;
using backend.DTOs.UoMDTOs;
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
    /// UNIT TEST: UoMService (Quản lý Đơn vị tính & Ràng buộc toàn vẹn)
    /// ============================================================================
    /// </summary>
    public class UoMServiceTests
    {
        private readonly IMapper _mapper;

        public UoMServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG, LỌC CATEGORY & TÌM KIẾM THEO SYNONYMS
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm theo từ đồng nghĩa (Synonyms), lọc theo CategoryId và phân trang.
        /// Kịch bản:
        /// - Tạo nhóm WEIGHT (Id=1) và nhóm VOLUME (Id=2).
        /// - Tạo ĐVT KG (Synonyms="ký, cân, kilogam"), G (Synonyms="gram, lạng"), L (Lít).
        /// - Tìm kiếm từ khóa "cân" -> Phải tìm ra đơn vị KG qua trường Synonyms.
        /// - Lọc theo CategoryId = 2 -> Phải trả về đơn vị Lít (L).
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldSearchBySynonymsAndFilterByCategory()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.AddRange(
                new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" },
                new UoMCategory { Id = 2, Code = "VOLUME", Name = "Thể tích" }
            );

            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "KG", Name = "Kilogram", CategoryId = 1, Synonyms = "ký, cân, kilogam", IsActive = true },
                new UoM { Id = 2, Code = "G", Name = "Gram", CategoryId = 1, Synonyms = "gram, lạng", IsActive = true },
                new UoM { Id = 3, Code = "L", Name = "Lít", CategoryId = 2, Synonyms = "lit, lít", IsActive = true }
            );
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);

            // Act 1: Tìm kiếm qua Synonyms "cân"
            var searchResult = await service.GetPagedAsync("cân", null, null, null, null, 1, 10);

            // Act 2: Lọc theo CategoryId = 2 (VOLUME)
            var categoryFilterResult = await service.GetPagedAsync(null, 2, null, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().Code.Should().Be("KG");

            categoryFilterResult.TotalRecords.Should().Be(1);
            categoryFilterResult.Items.First().Code.Should().Be("L");
        }
        #endregion

        #region TC02: LẤY CHI TIẾT ĐVT THEO ID KÈM CATEGORY
        /// <summary>
        /// TC02: Lấy chi tiết ĐVT theo ID, kiểm tra AutoMapper ánh xạ đúng CategoryCode và CategoryName.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldIncludeCategoryInfo()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var category = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            var uom = new UoM { Id = 10, Code = "KG", Name = "Kilogram", CategoryId = 1, Category = category };

            context.UoMCategories.Add(category);
            context.UoMs.Add(uom);
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(10);

            // Assert
            result.Should().NotBeNull();
            result!.Code.Should().Be("KG");
            result.CategoryId.Should().Be(1);
            result.CategoryCode.Should().Be("WEIGHT");
            result.CategoryName.Should().Be("Khối lượng");
        }
        #endregion

        #region TC03: CHẶN TRÙNG MÃ ĐVT (CASE-INSENSITIVE)
        /// <summary>
        /// TC03: Hệ thống phải ném InvalidOperationException khi tạo ĐVT có mã Code trùng lặp.
        /// </summary>
        [Fact]
        public async Task CreateAsync_DuplicateCode_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram", CategoryId = 1 });
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);
            var dto = new UoMCreateDto
            {
                Code = "  kg  ",
                Name = "Ký mới",
                CategoryId = 1
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại*");
        }
        #endregion

        #region TC04: CHẶN TẠO ĐVT KHI NHÓM KHÔNG TỒN TẠI
        /// <summary>
        /// TC04: Chặn tạo ĐVT khi CategoryId không tồn tại trong hệ thống.
        /// </summary>
        [Fact]
        public async Task CreateAsync_NonExistentCategory_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new UoMService(context, _mapper);
            var dto = new UoMCreateDto
            {
                Code = "BOX",
                Name = "Hộp",
                CategoryId = 999 // Không tồn tại
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Nhóm đơn vị tính không tồn tại*");
        }
        #endregion

        #region TC05: TẠO MỚI ĐVT THÀNH CÔNG
        /// <summary>
        /// TC05: Tạo mới ĐVT thành công, tự động chuẩn hóa Code viết hoa và lưu đầy đủ thông tin.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ValidDto_ShouldPersistAndReturnId()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);
            var dto = new UoMCreateDto
            {
                Code = "  ton  ",
                Name = "  Tấn  ",
                Synonyms = "  tan, tấn  ",
                CategoryId = 1,
                IsActive = true
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var entity = await context.UoMs.FindAsync(newId);
            entity.Should().NotBeNull();
            entity!.Code.Should().Be("TON");
            entity.Name.Should().Be("Tấn");
            entity.Synonyms.Should().Be("tan, tấn");
            entity.CategoryId.Should().Be(1);
            entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }
        #endregion

        #region TC06: CHẶN XÓA ĐVT LÀ BASE UOM CỦA NHÓM (SAFETY SHIELD 1)
        /// <summary>
        /// TC06: Chặn xóa ĐVT nếu đơn vị này đang đóng vai trò là Đơn vị cơ sở (BaseUoM) của một Category.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenIsBaseUoM_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kilogram" };
            var category = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng", BaseUoMId = 1 };

            context.UoMs.Add(uom);
            context.UoMCategories.Add(category);
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);

            // Act
            var act = async () => await service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Đơn vị gốc cho một nhóm đơn vị*");
        }
        #endregion

        #region TC07: CHẶN XÓA ĐVT ĐANG DÙNG TRONG QUY ĐỔI (SAFETY SHIELD 2 & 3)
        /// <summary>
        /// TC07: Chặn xóa ĐVT nếu đơn vị này đang tham gia vào một quy tắc quy đổi (FromUoM hoặc ToUoM).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenUsedInConversion_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "TON", Name = "Tấn", CategoryId = 1 },
                new UoM { Id = 2, Code = "KG", Name = "Kilogram", CategoryId = 1 }
            );
            context.UoMConversions.Add(new UoMConversion
            {
                Id = 1,
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 1000
            });
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);

            // Act
            var act = async () => await service.DeleteAsync(1);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Đang có quy tắc quy đổi liên kết*");
        }
        #endregion

        #region TC08: ĐẢO TRẠNG THÁI HOẠT ĐỘNG (TOGGLE ACTIVE)
        /// <summary>
        /// TC08: Đảo trạng thái hoạt động của Đơn vị tính giữa Kích hoạt và Tạm khóa.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ExistingId_ShouldInvertStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.Add(new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram", CategoryId = 1, IsActive = true });
            await context.SaveChangesAsync();

            var service = new UoMService(context, _mapper);

            // Act
            var newStatus = await service.ToggleActiveAsync(1);

            // Assert
            newStatus.Should().BeFalse();

            var updated = await context.UoMs.FindAsync(1);
            updated!.IsActive.Should().BeFalse();
            updated.UpdatedAt.Should().NotBeNull();
        }
        #endregion
    }
}
