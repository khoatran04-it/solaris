using AutoMapper;
using backend.Data;
using backend.DTOs.UoMConversionDTOs;
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
    /// 📦 MODULE 2: UNIT OF MEASURE (UoM) MASTER DATA
    /// 🧪 UNIT TEST: UoMConversionService (Quản lý Quy tắc Quy đổi Đơn vị tính)
    /// ============================================================================
    /// </summary>
    public class UoMConversionServiceTests
    {
        private readonly IMapper _mapper;

        public UoMConversionServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & LỌC THEO QUY ĐỔI TIÊU CHUẨN HOẶC ĐẶC THÙ
        /// <summary>
        /// TC01: Lọc danh sách quy đổi theo tiêu chuẩn (IsStandard = true) hoặc theo sản phẩm cụ thể.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterByStandardAndProduct()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            var uom1 = new UoM { Id = 1, Code = "TON", Name = "Tấn", CategoryId = 1 };
            var uom2 = new UoM { Id = 2, Code = "KG", Name = "Kilogram", CategoryId = 1 };
            var product = new Product
            {
                Id = 100,
                Code = "PROD-01",
                Name = "Táo Envy",
                Slug = "tao-envy",
                BaseUoMId = 2,
                CategoryId = 1
            };

            context.UoMCategories.Add(cat);
            context.UoMs.AddRange(uom1, uom2);
            context.Products.Add(product);

            // Quy đổi 1: Tiêu chuẩn hệ thống (1 Tấn = 1000 Kg)
            context.UoMConversions.Add(new UoMConversion
            {
                Id = 1,
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 1000,
                ProductId = null
            });

            // Quy đổi 2: Đặc thù sản phẩm Táo Envy (1 Thùng = 18 Kg)
            context.UoMConversions.Add(new UoMConversion
            {
                Id = 2,
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 18,
                ProductId = 100
            });
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);

            // Act 1: Lọc quy đổi tiêu chuẩn
            var standardResult = await service.GetPagedAsync(null, null, true, null, null, null, 1, 10);

            // Act 2: Lọc quy đổi theo sản phẩm 100
            var productResult = await service.GetPagedAsync(null, 100, null, null, null, null, 1, 10);

            // Assert
            standardResult.TotalRecords.Should().Be(1);
            standardResult.Items.First().ProductId.Should().BeNull();
            standardResult.Items.First().IsStandard.Should().BeTrue();

            productResult.TotalRecords.Should().Be(1);
            productResult.Items.First().ProductId.Should().Be(100);
            productResult.Items.First().IsStandard.Should().BeFalse();
        }
        #endregion

        #region TC02: LẤY CHI TIẾT KÈM DIỄN GIẢI NGỮ NGHĨA (SEMANTIC DESCRIPTION)
        /// <summary>
        /// TC02: Kiểm tra việc tính toán và hiển thị SemanticDescription (VD: "1 Tấn = 1000 Kilogram").
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WhenFound_ShouldGenerateCorrectSemanticDescription()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            var uom1 = new UoM { Id = 1, Code = "TON", Name = "Tấn", CategoryId = 1 };
            var uom2 = new UoM { Id = 2, Code = "KG", Name = "Kilogram", CategoryId = 1 };
            var conv = new UoMConversion
            {
                Id = 1,
                FromUoMId = 1,
                FromUoM = uom1,
                ToUoMId = 2,
                ToUoM = uom2,
                ConversionFactor = 1000
            };

            context.UoMCategories.Add(cat);
            context.UoMs.AddRange(uom1, uom2);
            context.UoMConversions.Add(conv);
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.FromUoMCode.Should().Be("TON");
            result.ToUoMCode.Should().Be("KG");
            result.SemanticDescription.Should().Be("1 Tấn = 1000 Kilogram");
        }
        #endregion

        #region TC03: CHẶN HỆ SỐ QUY ĐỔI <= 0
        /// <summary>
        /// TC03: Hệ số quy đổi bắt buộc phải lớn hơn 0, nếu nhỏ hơn hoặc bằng 0 phải ném InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task CreateAsync_InvalidFactor_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            var uom1 = new UoM { Id = 1, Code = "TON", Name = "Tấn", CategoryId = 1 };
            var uom2 = new UoM { Id = 2, Code = "KG", Name = "Kilogram", CategoryId = 1 };
            context.UoMCategories.Add(cat);
            context.UoMs.AddRange(uom1, uom2);
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);
            var dto = new UoMConversionCreateDto
            {
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 0 // Không hợp lệ
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Hệ số quy đổi phải lớn hơn 0*");
        }
        #endregion

        #region TC04: CHẶN QUY ĐỔI VÒNG LẶP (FROM == TO)
        /// <summary>
        /// TC04: Không cho phép thiết lập quy đổi từ một đơn vị về chính nó (FromUoMId == ToUoMId).
        /// </summary>
        [Fact]
        public async Task CreateAsync_SelfLoop_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kilogram", CategoryId = 1 };
            context.UoMCategories.Add(cat);
            context.UoMs.Add(uom);
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);
            var dto = new UoMConversionCreateDto
            {
                FromUoMId = 1,
                ToUoMId = 1, // Trùng
                ConversionFactor = 1
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Lỗi Vòng Lặp*");
        }
        #endregion

        #region TC05: CHẶN QUY ĐỔI CHÉO NHÓM TIÊU CHUẨN
        /// <summary>
        /// TC05: Quy đổi tiêu chuẩn toàn hệ thống (ProductId = null) bắt buộc phải cùng CategoryId.
        /// Chặn quy đổi giữa nhóm Khối lượng và nhóm Thể tích nếu không có gắn Sản phẩm.
        /// </summary>
        [Fact]
        public async Task CreateAsync_StandardCrossCategory_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.AddRange(
                new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" },
                new UoMCategory { Id = 2, Code = "VOLUME", Name = "Thể tích" }
            );
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "KG", Name = "Kilogram", CategoryId = 1 },
                new UoM { Id = 2, Code = "L", Name = "Lít", CategoryId = 2 }
            );
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);
            var dto = new UoMConversionCreateDto
            {
                FromUoMId = 1, // Khối lượng
                ToUoMId = 2,   // Thể tích
                ConversionFactor = 1,
                ProductId = null // Quy đổi tiêu chuẩn -> Sai logic
            };

            // Act
            var act = async () => await service.CreateAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể quy đổi tiêu chuẩn chéo giữa 2 nhóm*");
        }
        #endregion

        #region TC06: CHO PHÉP QUY ĐỔI CHÉO NHÓM NẾU GẮN SẢN PHẨM ĐẶC THÙ
        /// <summary>
        /// TC06: Cho phép quy đổi khác Category nếu gắn kèm một Sản phẩm đặc thù (VD: 1 Thùng Nước khoáng = 24 Chai).
        /// </summary>
        [Fact]
        public async Task CreateAsync_ProductSpecificCrossCategory_ShouldSucceed()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMCategories.AddRange(
                new UoMCategory { Id = 1, Code = "PACKAGING", Name = "Đóng gói" },
                new UoMCategory { Id = 2, Code = "VOLUME", Name = "Thể tích" }
            );
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "BOX", Name = "Thùng", CategoryId = 1 },
                new UoM { Id = 2, Code = "BTL", Name = "Chai", CategoryId = 2 }
            );
            context.Products.Add(new Product
            {
                Id = 10,
                Code = "WATER",
                Name = "Nước khoáng Lavie",
                Slug = "nuoc-khoang-lavie",
                BaseUoMId = 2,
                CategoryId = 1
            });
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);
            var dto = new UoMConversionCreateDto
            {
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 24,
                ProductId = 10 // Có sản phẩm đặc thù -> Hợp lệ
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);
            var entity = await context.UoMConversions.FindAsync(newId);
            entity.Should().NotBeNull();
            entity!.ConversionFactor.Should().Be(24);
            entity.ProductId.Should().Be(10);
        }
        #endregion

        #region TC07: CHẶN TRÙNG LẶP QUY TẮC QUY ĐỔI
        /// <summary>
        /// TC07: Chặn tạo trùng lặp quy tắc đã có trên cùng một cặp FromUoM, ToUoM và Product.
        /// </summary>
        [Fact]
        public async Task CreateAsync_DuplicateRule_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            context.UoMCategories.Add(cat);
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "TON", Name = "Tấn", CategoryId = 1 },
                new UoM { Id = 2, Code = "KG", Name = "Kilogram", CategoryId = 1 }
            );
            context.UoMConversions.Add(new UoMConversion
            {
                Id = 1,
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 1000,
                ProductId = null
            });
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);
            var duplicateDto = new UoMConversionCreateDto
            {
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 1000,
                ProductId = null
            };

            // Act
            var act = async () => await service.CreateAsync(duplicateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Quy tắc chuyển đổi này đã tồn tại*");
        }
        #endregion

        #region TC08: TẠO MỚI & CẬP NHẬT QUY TẮC THÀNH CÔNG
        /// <summary>
        /// TC08: Cập nhật hệ số quy đổi và trạng thái của quy tắc thành công.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ValidDto_ShouldUpdateSuccessfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            context.UoMCategories.Add(cat);
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "TON", Name = "Tấn", CategoryId = 1 },
                new UoM { Id = 2, Code = "KG", Name = "Kilogram", CategoryId = 1 }
            );
            context.UoMConversions.Add(new UoMConversion
            {
                Id = 1,
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 900 // Ban đầu sai
            });
            await context.SaveChangesAsync();

            var service = new UoMConversionService(context, _mapper);
            var updateDto = new UoMConversionUpdateDto
            {
                FromUoMId = 1,
                ToUoMId = 2,
                ConversionFactor = 1000, // Cập nhật đúng
                IsActive = true
            };

            // Act
            var result = await service.UpdateAsync(1, updateDto);

            // Assert
            result.Should().BeTrue();

            var updatedEntity = await context.UoMConversions.FindAsync(1);
            updatedEntity!.ConversionFactor.Should().Be(1000);
            updatedEntity.UpdatedAt.Should().NotBeNull();
        }
        #endregion
    }
}
