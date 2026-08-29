using AutoMapper;
using backend.DTOs.PromotionCampaignDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module07_PromotionCampaign
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 07: PROMOTION CAMPAIGN & PROMOTION VARIANT MANAGEMENT
    /// 🧪 TEST SUITE: PromotionCampaignServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Quản lý Chiến Dịch Khuyến Mãi (Promotion Campaigns):
    /// - Quản lý cấu hình chiến dịch (Flash Sale, Giảm giá theo % hoặc Tiền mặt)
    /// - Tự động tạo và đồng bộ SEO Slug
    /// - Điều phối danh sách biến thể (SKU) tham gia chiến dịch
    /// - Trích xuất Enriched Data (Flatten Giá mặc định, Đơn vị tính, Hình ảnh)
    /// - Ràng buộc thời gian hiệu lực và mức giảm giá
    /// - Soft Delete và dọn dẹp liên kết trung gian
    /// - Đảm bảo bọc toàn bộ mutations trong Database Transaction
    /// </summary>
    public class PromotionCampaignServiceTests
    {
        private readonly IMapper _mapper;

        public PromotionCampaignServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        /// <summary>
        /// Chuẩn bị dữ liệu mẫu cho Product, ProductVariant, UoM và Prices để phục vụ kiểm thử.
        /// </summary>
        private static async Task SeedSampleProductsAndVariantsAsync(Data.SolarisDbContext context)
        {
            var uomCategory = new UoMCategory { Id = 1, Code = "WEIGHT", Name = "Khối lượng" };
            var uomKg = new UoM { Id = 1, Code = "KG", Name = "Kilogram", CategoryId = 1 };
            var uomBox = new UoM { Id = 2, Code = "BOX", Name = "Hộp 1kg", CategoryId = 1 };
            context.UoMCategories.Add(uomCategory);
            context.UoMs.AddRange(uomKg, uomBox);

            var product = new Product
            {
                Id = 1,
                Code = "SP001",
                Name = "Xoài Cát Hòa Lộc",
                BaseUoMId = 1
            };
            context.Products.Add(product);

            var variant1 = new ProductVariant
            {
                Id = 101,
                ProductId = 1,
                Code = "SKU-XOAI-500G",
                Name = "Xoài Cát Hòa Lộc 500g",
                ImagePath = "/images/xoai-500g.png",
                IsActive = true,
                IsDeleted = false
            };

            var variant2 = new ProductVariant
            {
                Id = 102,
                ProductId = 1,
                Code = "SKU-XOAI-1KG",
                Name = "Xoài Cát Hòa Lộc 1kg",
                ImagePath = "/images/xoai-1kg.png",
                IsActive = true,
                IsDeleted = false
            };

            var variantDeleted = new ProductVariant
            {
                Id = 103,
                ProductId = 1,
                Code = "SKU-XOAI-OLD",
                Name = "Xoài Cát Hòa Lộc Cũ",
                IsActive = false,
                IsDeleted = true
            };

            context.ProductVariants.AddRange(variant1, variant2, variantDeleted);

            // Bảng giá cho variant 101 (có 1 giá mặc định)
            var price1 = new ProductVariantPrice
            {
                Id = 1,
                VariantId = 101,
                UoMId = 1,
                Price = 60000m,
                IsDefault = true
            };
            var price2 = new ProductVariantPrice
            {
                Id = 2,
                VariantId = 101,
                UoMId = 2,
                Price = 110000m,
                IsDefault = false
            };

            // Bảng giá cho variant 102
            var price3 = new ProductVariantPrice
            {
                Id = 3,
                VariantId = 102,
                UoMId = 1,
                Price = 120000m,
                IsDefault = true
            };

            context.ProductVariantPrices.AddRange(price1, price2, price3);
            await context.SaveChangesAsync();
        }
        #endregion

        // ============================================================================
        // 1. TEST CASES: GET ALL & GET PAGED (QUERIES)
        // ============================================================================
        #region Query Tests

        /// <summary>
        /// TC01: Lấy toàn bộ danh sách chiến dịch, sắp xếp theo CreatedAt giảm dần và hỗ trợ lọc isActiveOnly.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllCampaigns_WithAppliedVariants_And_RespectActiveFilter()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleProductsAndVariantsAsync(context);

            var campaign1 = new PromotionCampaign
            {
                Id = 1,
                Name = "Flash Sale Cuối Tuần",
                Slug = "flash-sale-cuoi-tuan",
                IsPercentage = true,
                DiscountValue = 15m,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(2),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            };

            var campaign2 = new PromotionCampaign
            {
                Id = 2,
                Name = "Xả Kho Mùa Mưa",
                Slug = "xa-kho-mua-mua",
                IsPercentage = false,
                DiscountValue = 20000m,
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(10),
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            context.PromotionCampaigns.AddRange(campaign1, campaign2);
            context.PromotionVariants.Add(new PromotionVariant
            {
                Id = 1,
                PromotionCampaignId = 1,
                VariantId = 101
            });
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act 1: Lấy tất cả
            var allResult = (await service.GetAllListAsync(isActiveOnly: false)).ToList();

            // Act 2: Chỉ lấy chiến dịch active
            var activeResult = (await service.GetAllListAsync(isActiveOnly: true)).ToList();

            // Assert
            allResult.Should().HaveCount(2);
            allResult[0].Id.Should().Be(2); // Sắp xếp CreatedAt giảm dần
            allResult[1].Id.Should().Be(1);
            allResult[1].AppliedVariants.Should().HaveCount(1);
            allResult[1].AppliedVariants[0].VariantCode.Should().Be("SKU-XOAI-500G");

            activeResult.Should().HaveCount(1);
            activeResult[0].Id.Should().Be(1);
        }

        /// <summary>
        /// TC02: Phân trang danh sách chiến dịch, tìm kiếm theo tên và lọc theo khoảng thời gian hiệu lực.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterBySearch_ActiveStatus_And_DateRange()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            context.PromotionCampaigns.AddRange(
                new PromotionCampaign
                {
                    Id = 1,
                    Name = "Siêu Sale Trung Thu",
                    Slug = "sieu-sale-trung-thu",
                    IsPercentage = true,
                    DiscountValue = 20m,
                    StartDate = new DateTime(2026, 9, 10),
                    EndDate = new DateTime(2026, 9, 20),
                    IsActive = true,
                    CreatedAt = now.AddDays(-2)
                },
                new PromotionCampaign
                {
                    Id = 2,
                    Name = "Khuyến Mãi Khai Giảng",
                    Slug = "khuyen-mai-khai-giang",
                    IsPercentage = true,
                    DiscountValue = 10m,
                    StartDate = new DateTime(2026, 9, 1),
                    EndDate = new DateTime(2026, 9, 5),
                    IsActive = true,
                    CreatedAt = now.AddDays(-1)
                },
                new PromotionCampaign
                {
                    Id = 3,
                    Name = "Flash Sale Xả Hàng",
                    Slug = "flash-sale-xa-hang",
                    IsPercentage = false,
                    DiscountValue = 50000m,
                    StartDate = new DateTime(2026, 10, 1),
                    EndDate = new DateTime(2026, 10, 10),
                    IsActive = false,
                    CreatedAt = now
                }
            );
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act 1: Tìm kiếm theo từ khóa "Sale"
            var searchResult = await service.GetPagedAsync("Sale", null, null, null, 1, 10);

            // Act 2: Lọc theo thời gian trong tháng 9
            var dateResult = await service.GetPagedAsync(
                null,
                true,
                new DateTime(2026, 9, 1),
                new DateTime(2026, 9, 30),
                1,
                10
            );

            // Assert
            searchResult.TotalRecords.Should().Be(2);
            searchResult.Items.Select(x => x.Id).Should().Contain(new[] { 1, 3 });

            dateResult.TotalRecords.Should().Be(2);
            dateResult.Items.Select(x => x.Id).Should().Contain(new[] { 1, 2 });
        }

        /// <summary>
        /// TC03: Lấy chi tiết chiến dịch theo ID, tự động Flatten thông tin sản phẩm và giá mặc định (Enriched Data).
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnEnrichedCampaign_WithAppliedVariants_And_DefaultPrice()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleProductsAndVariantsAsync(context);

            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Giảm Giá Trái Cây Mùa Hè",
                Slug = "giam-gia-trai-cay-mua-he",
                Description = "Áp dụng cho xoài Hòa Lộc",
                IsPercentage = true,
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };
            context.PromotionCampaigns.Add(campaign);

            // Gắn variant 101 và 102
            context.PromotionVariants.AddRange(
                new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 101 },
                new PromotionVariant { Id = 2, PromotionCampaignId = 1, VariantId = 102 }
            );
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(1);
            var notFoundResult = await service.GetByIdAsync(999);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Giảm Giá Trái Cây Mùa Hè");
            result.AppliedVariants.Should().HaveCount(2);

            // Kiểm tra Flatten logic của variant 101
            var applied101 = result.AppliedVariants.FirstOrDefault(v => v.VariantId == 101);
            applied101.Should().NotBeNull();
            applied101!.VariantCode.Should().Be("SKU-XOAI-500G");
            applied101.ProductName.Should().Be("Xoài Cát Hòa Lộc");
            applied101.ImagePath.Should().Be("/images/xoai-500g.png");
            applied101.DefaultPrice.Should().Be(60000m);
            applied101.DefaultUoMName.Should().Be("Kilogram");

            notFoundResult.Should().BeNull();
        }

        #endregion

        // ============================================================================
        // 2. TEST CASES: CREATE & UPDATE (COMMANDS)
        // ============================================================================
        #region Command Tests

        /// <summary>
        /// TC04: Tạo mới chiến dịch thành công, tự động sinh Slug và gán danh sách biến thể khởi tạo.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateCampaign_And_GenerateSlug_And_AttachVariants()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleProductsAndVariantsAsync(context);

            var service = new PromotionCampaignService(context, _mapper);

            var createDto = new PromotionCampaignCreateDto
            {
                Name = "Khuyến Mãi Đặc Biệt Tháng 8",
                Description = "Giảm giá sâu các mặt hàng",
                IsPercentage = true,
                DiscountValue = 25m,
                StartDate = DateTime.UtcNow.AddHours(1),
                EndDate = DateTime.UtcNow.AddDays(5),
                IsActive = true,
                VariantIds = new List<int> { 101, 102 }
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var savedCampaign = await context.PromotionCampaigns
                .Include(c => c.PromotionVariants)
                .FirstOrDefaultAsync(c => c.Id == newId);

            savedCampaign.Should().NotBeNull();
            savedCampaign!.Name.Should().Be("Khuyến Mãi Đặc Biệt Tháng 8");
            savedCampaign.Slug.Should().Be("khuyen-mai-dac-biet-thang-8");
            savedCampaign.IsPercentage.Should().BeTrue();
            savedCampaign.DiscountValue.Should().Be(25m);
            savedCampaign.PromotionVariants.Should().HaveCount(2);
            savedCampaign.PromotionVariants.Select(pv => pv.VariantId).Should().Contain(new[] { 101, 102 });
        }

        /// <summary>
        /// TC05: Kiểm tra các lỗi nghiệp vụ khi tạo mới chiến dịch (Tên rỗng, ngày không hợp lệ, mức giảm sai, trùng tên).
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenValidationFails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.PromotionCampaigns.Add(new PromotionCampaign
            {
                Id = 1,
                Name = "Flash Sale Tồn Tại",
                Slug = "flash-sale-ton-tai",
                IsPercentage = true,
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            });
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // 1. Tên rỗng
            var emptyNameDto = new PromotionCampaignCreateDto
            {
                Name = "   ",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                IsPercentage = true,
                DiscountValue = 10m
            };
            var actEmptyName = async () => await service.CreateAsync(emptyNameDto);
            await actEmptyName.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*không được để trống*");

            // 2. StartDate >= EndDate
            var invalidDateDto = new PromotionCampaignCreateDto
            {
                Name = "Chiến Dịch Lỗi Ngày",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(2),
                IsPercentage = true,
                DiscountValue = 10m
            };
            var actInvalidDate = async () => await service.CreateAsync(invalidDateDto);
            await actInvalidDate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Ngày bắt đầu phải trước ngày kết thúc*");

            // 3. % Giảm vượt quá 100% hoặc <= 0
            var invalidPercentDto = new PromotionCampaignCreateDto
            {
                Name = "Chiến Dịch Lỗi %",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                IsPercentage = true,
                DiscountValue = 150m // > 100
            };
            var actInvalidPercent = async () => await service.CreateAsync(invalidPercentDto);
            await actInvalidPercent.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*từ 0.01% đến 100%*");

            // 4. Giảm tiền mặt <= 0
            var invalidAmountDto = new PromotionCampaignCreateDto
            {
                Name = "Chiến Dịch Lỗi Tiền",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                IsPercentage = false,
                DiscountValue = 0m
            };
            var actInvalidAmount = async () => await service.CreateAsync(invalidAmountDto);
            await actInvalidAmount.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mức giảm tiền mặt phải lớn hơn 0*");

            // 5. Trùng tên chiến dịch (không phân biệt hoa thường)
            var duplicateNameDto = new PromotionCampaignCreateDto
            {
                Name = "FLASH SALE TỒN TẠI",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                IsPercentage = true,
                DiscountValue = 10m
            };
            var actDuplicateName = async () => await service.CreateAsync(duplicateNameDto);
            await actDuplicateName.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã tồn tại trong hệ thống*");
        }

        /// <summary>
        /// TC06: Cập nhật thông tin vỏ chiến dịch và cập nhật lại SEO Slug tương ứng.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateCampaignFields_And_Slug_Successfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Tên Cũ",
                Slug = "ten-cu",
                Description = "Mô tả cũ",
                IsPercentage = true,
                DiscountValue = 10m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2),
                IsActive = true
            };
            context.PromotionCampaigns.Add(campaign);
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            var updateDto = new PromotionCampaignUpdateDto
            {
                Name = "Tên Mới Được Cập Nhật",
                Description = "Mô tả mới",
                IsPercentage = false,
                DiscountValue = 30000m,
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(10),
                IsActive = true
            };

            // Act
            var result = await service.UpdateAsync(1, updateDto);

            // Assert
            result.Should().BeTrue();

            var updatedCampaign = await context.PromotionCampaigns.FindAsync(1);
            updatedCampaign.Should().NotBeNull();
            updatedCampaign!.Name.Should().Be("Tên Mới Được Cập Nhật");
            updatedCampaign.Slug.Should().Be("ten-moi-duoc-cap-nhat");
            updatedCampaign.Description.Should().Be("Mô tả mới");
            updatedCampaign.IsPercentage.Should().BeFalse();
            updatedCampaign.DiscountValue.Should().Be(30000m);
        }

        /// <summary>
        /// TC07: Ném KeyNotFoundException khi cập nhật chiến dịch không tồn tại, hoặc InvalidOperationException khi trùng tên.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldThrowExceptions_OnNotFound_Or_DuplicateName()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.PromotionCampaigns.AddRange(
                new PromotionCampaign
                {
                    Id = 1,
                    Name = "Chiến Dịch Một",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(2)
                },
                new PromotionCampaign
                {
                    Id = 2,
                    Name = "Chiến Dịch Hai",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(2)
                }
            );
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // 1. Không tìm thấy ID
            var notFoundDto = new PromotionCampaignUpdateDto
            {
                Name = "Tên Bất Kỳ",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                IsPercentage = true,
                DiscountValue = 10m
            };
            var actNotFound = async () => await service.UpdateAsync(999, notFoundDto);
            await actNotFound.Should().ThrowAsync<KeyNotFoundException>();

            // 2. Trùng tên với chiến dịch khác (Id 1 đổi tên thành "Chiến Dịch Hai")
            var duplicateDto = new PromotionCampaignUpdateDto
            {
                Name = "chiến dịch hai",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                IsPercentage = true,
                DiscountValue = 10m
            };
            var actDuplicate = async () => await service.UpdateAsync(1, duplicateDto);
            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã bị trùng*");
        }

        #endregion

        // ============================================================================
        // 3. TEST CASES: DELETE & TOGGLE ACTIVE
        // ============================================================================
        #region Delete & Toggle Tests

        /// <summary>
        /// TC08: Xóa mềm chiến dịch và tự động dọn dẹp các liên kết PromotionVariants.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldSoftDeleteCampaign_And_CleanupPromotionVariants()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Chiến Dịch Cần Xóa",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            context.PromotionCampaigns.Add(campaign);
            context.PromotionVariants.AddRange(
                new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 101 },
                new PromotionVariant { Id = 2, PromotionCampaignId = 1, VariantId = 102 }
            );
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act
            var result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            // Chiến dịch bị xóa mềm (không truy vấn thấy qua DbSet thông thường do Query Filter hoặc IsDeleted = true)
            var deletedCampaign = await context.PromotionCampaigns
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == 1);

            deletedCampaign.Should().NotBeNull();
            deletedCampaign!.IsDeleted.Should().BeTrue();
            deletedCampaign.DeletedAt.Should().NotBeNull();

            // Liên kết biến thể bị xóa sạch khỏi bảng trung gian
            var remainingLinks = await context.PromotionVariants
                .Where(pv => pv.PromotionCampaignId == 1)
                .ToListAsync();
            remainingLinks.Should().BeEmpty();
        }

        /// <summary>
        /// TC09: Bật / Tắt trạng thái hoạt động ToggleActiveAsync thành công.
        /// </summary>
        [Fact]
        public async Task ToggleActiveAsync_ShouldToggleActiveStatus_And_ThrowIfNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Flash Sale Khẩn Cấp",
                IsActive = true,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            context.PromotionCampaigns.Add(campaign);
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act 1: Tắt active (true -> false)
            var result1 = await service.ToggleActiveAsync(1);
            var status1 = (await context.PromotionCampaigns.FindAsync(1))!.IsActive;

            // Act 2: Bật lại active (false -> true)
            var result2 = await service.ToggleActiveAsync(1);
            var status2 = (await context.PromotionCampaigns.FindAsync(1))!.IsActive;

            // Assert
            result1.Should().BeTrue();
            status1.Should().BeFalse();

            result2.Should().BeTrue();
            status2.Should().BeTrue();

            // Kiểm tra ID không tồn tại
            var actNotFound = async () => await service.ToggleActiveAsync(999);
            await actNotFound.Should().ThrowAsync<KeyNotFoundException>();
        }

        #endregion

        // ============================================================================
        // 4. TEST CASES: VARIANT MAPPING (ĐẶC THÙ BẢNG TRUNG GIAN)
        // ============================================================================
        #region Variant Mapping Tests

        /// <summary>
        /// TC10: Đồng bộ hàng loạt sản phẩm vào chiến dịch (AddVariantsToCampaignAsync) - Thêm mới và gỡ bỏ sản phẩm bị bỏ chọn.
        /// </summary>
        [Fact]
        public async Task AddVariantsToCampaignAsync_ShouldSynchronizeVariants_MultiSelect()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleProductsAndVariantsAsync(context);

            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Chiến Dịch Đồng Bộ",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            context.PromotionCampaigns.Add(campaign);

            // Ban đầu chiến dịch có variant 101
            context.PromotionVariants.Add(new PromotionVariant
            {
                Id = 1,
                PromotionCampaignId = 1,
                VariantId = 101
            });
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act: Đồng bộ lại danh sách: bỏ 101, thêm 102 và thử truyền thêm 103 (đã bị xóa mềm)
            var dto = new ApplyVariantsToCampaignDto
            {
                VariantIds = new List<int> { 102, 103 }
            };
            var result = await service.AddVariantsToCampaignAsync(1, dto);

            // Assert
            result.Should().BeTrue();

            var updatedLinks = await context.PromotionVariants
                .Where(pv => pv.PromotionCampaignId == 1)
                .ToListAsync();

            // 101 bị xóa, 102 được thêm, 103 (bị soft delete) bị bỏ qua
            updatedLinks.Should().HaveCount(1);
            updatedLinks[0].VariantId.Should().Be(102);
        }

        /// <summary>
        /// TC11: Gỡ bỏ danh sách sản phẩm chỉ định khỏi chiến dịch (RemoveVariantsFromCampaignAsync).
        /// </summary>
        [Fact]
        public async Task RemoveVariantsFromCampaignAsync_ShouldRemoveSelectedVariants_Successfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedSampleProductsAndVariantsAsync(context);

            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Chiến Dịch Xóa Bớt Sản Phẩm",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2)
            };
            context.PromotionCampaigns.Add(campaign);

            context.PromotionVariants.AddRange(
                new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 101 },
                new PromotionVariant { Id = 2, PromotionCampaignId = 1, VariantId = 102 }
            );
            await context.SaveChangesAsync();

            var service = new PromotionCampaignService(context, _mapper);

            // Act: Gỡ bỏ variant 101
            var removeDto = new ApplyVariantsToCampaignDto
            {
                VariantIds = new List<int> { 101 }
            };
            var result = await service.RemoveVariantsFromCampaignAsync(1, removeDto);

            // Assert
            result.Should().BeTrue();

            var remainingLinks = await context.PromotionVariants
                .Where(pv => pv.PromotionCampaignId == 1)
                .ToListAsync();

            remainingLinks.Should().HaveCount(1);
            remainingLinks[0].VariantId.Should().Be(102);

            // Kiểm tra ID chiến dịch không tồn tại
            var actNotFound = async () => await service.RemoveVariantsFromCampaignAsync(999, removeDto);
            await actNotFound.Should().ThrowAsync<KeyNotFoundException>();
        }

        #endregion
    }
}
