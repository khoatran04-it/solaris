using AutoMapper;
using backend.DTOs.ShopDTOs;
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
    /// 🧪 UNIT TEST: ShopProductService (Dữ liệu Cửa hàng B2C/B2B & Tìm Kiếm Đa Chiều)
    /// ============================================================================
    /// </summary>
    public class ShopProductServiceTests
    {
        private readonly IMapper _mapper;

        public ShopProductServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: TÌM KIẾM SẢN PHẨM CỬA HÀNG ĐA TẦNG (TỪ KHÓA, SLUG NHÓM/DANH MỤC, XUẤT XỨ, CHỨNG NHẬN, KHOẢNG GIÁ, SẮP XẾP)
        /// <summary>
        /// TC01: Kiểm tra tính năng tìm kiếm sản phẩm B2C với bộ lọc phức hợp: từ khóa, categoryGroupSlug, categorySlug, 
        /// EAV origin/certification, min/max price, tính tồn kho khả dụng từ kho còn hạn sử dụng, và sắp xếp theo giá / giảm giá.
        /// </summary>
        [Fact]
        public async Task GetProductsAsync_ShouldFilterAndCalculateCardPricesAndStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var group = new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm tươi", Slug = "thuc-pham-tuoi", IsActive = true };
            var cat = new ProductCategory { Id = 10, Code = "FRUIT", Name = "Trái cây", Slug = "trai-cay", CategoryGroupId = 1, IsActive = true };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var attrOrigin = new AttributeDefinition { Id = 100, Name = "Xuất xứ", DataType = "TEXT", IsActive = true };
            var attrCert = new AttributeDefinition { Id = 101, Name = "Tiêu chuẩn chứng nhận", DataType = "TEXT", IsActive = true };
            var attrBrix = new AttributeDefinition { Id = 102, Name = "Độ ngọt Brix", DataType = "NUMBER", IsActive = true };

            context.ProductCategoryGroups.Add(group);
            context.ProductCategories.Add(cat);
            context.UoMs.Add(uom);
            context.AttributeDefinitions.AddRange(attrOrigin, attrCert, attrBrix);

            // Sản phẩm 1: Táo Envy (Có tồn kho, có giảm giá 10%)
            var prod1 = new Product
            {
                Id = 1,
                Code = "PROD-TAO",
                Name = "Táo Envy New Zealand",
                Slug = "tao-envy-new-zealand",
                CategoryId = 10,
                BaseUoMId = 1,
                IsActive = true
            };
            var var1 = new ProductVariant { Id = 10, Code = "SKU-TAO-1KG", Name = "Táo Envy 1Kg", ProductId = 1, IsActive = true };
            var price1 = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 100000m, IsDefault = true, IsActive = true };

            var promo1 = new PromotionCampaign
            {
                Id = 1,
                Name = "Giảm 10%",
                IsPercentage = true,
                DiscountValue = 10,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(2),
                IsActive = true
            };
            var promoVar1 = new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 10 };

            var attrVal1 = new ProductAttribute { Id = 1, VariantId = 10, AttributeDefinitionId = 100, AttributeValue = "New Zealand", IsActive = true };
            var attrVal2 = new ProductAttribute { Id = 2, VariantId = 10, AttributeDefinitionId = 101, AttributeValue = "GlobalGAP", IsActive = true };
            var attrVal3 = new ProductAttribute { Id = 3, VariantId = 10, AttributeDefinitionId = 102, AttributeValue = "15", IsActive = true };

            var batch1 = new ProductBatch { Id = 1, BatchCode = "BATCH-TAO", VariantId = 10, ExpiryDate = now.AddMonths(1) };
            var inv1 = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, BatchId = 1, QuantityAvailable = 50 };

            // Sản phẩm 2: Cam Sành (Giá 40,000, Xuất xứ Hàm Yên, Tiêu chuẩn VietGAP)
            var prod2 = new Product
            {
                Id = 2,
                Code = "PROD-CAM",
                Name = "Cam Sành Hàm Yên",
                Slug = "cam-sanh-ham-yen",
                CategoryId = 10,
                BaseUoMId = 1,
                IsActive = true
            };
            var var2 = new ProductVariant { Id = 20, Code = "SKU-CAM-1KG", Name = "Cam Sành 1Kg", ProductId = 2, IsActive = true };
            var price2 = new ProductVariantPrice { Id = 2, VariantId = 20, UoMId = 1, Price = 40000m, IsDefault = true, IsActive = true };
            var attrVal4 = new ProductAttribute { Id = 4, VariantId = 20, AttributeDefinitionId = 100, AttributeValue = "Hàm Yên", IsActive = true };
            var attrVal5 = new ProductAttribute { Id = 5, VariantId = 20, AttributeDefinitionId = 101, AttributeValue = "VietGAP", IsActive = true };
            var inv2 = new WarehouseInventory { Id = 2, WarehouseId = 1, VariantId = 20, QuantityAvailable = 20 };

            context.Products.AddRange(prod1, prod2);
            context.ProductVariants.AddRange(var1, var2);
            context.ProductVariantPrices.AddRange(price1, price2);
            context.PromotionCampaigns.Add(promo1);
            context.PromotionVariants.Add(promoVar1);
            context.ProductAttributes.AddRange(attrVal1, attrVal2, attrVal3, attrVal4, attrVal5);
            context.ProductBatches.Add(batch1);
            context.WarehouseInventories.AddRange(inv1, inv2);
            await context.SaveChangesAsync();

            var service = new ShopProductService(context, _mapper);

            // Act 1: Tìm theo từ khóa "envy"
            var resSearch = await service.GetProductsAsync(new ShopProductFilterParams { Search = "envy" });

            // Act 2: Lọc theo CategoryGroupSlug = "thuc-pham-tuoi"
            var resGroup = await service.GetProductsAsync(new ShopProductFilterParams { CategoryGroupSlug = "thuc-pham-tuoi" });

            // Act 3: Lọc theo Origin = "New Zealand"
            var resOrigin = await service.GetProductsAsync(new ShopProductFilterParams { Origin = "New Zealand" });

            // Act 4: Lọc theo Certification = "VietGAP"
            var resCert = await service.GetProductsAsync(new ShopProductFilterParams { Certification = "VietGAP" });

            // Act 5: Sắp xếp theo giá tăng dần (price-asc)
            var resSortPrice = await service.GetProductsAsync(new ShopProductFilterParams { SortBy = "price-asc" });

            // Assert
            resSearch.TotalRecords.Should().Be(1);
            resSearch.Items.First().Slug.Should().Be("tao-envy-new-zealand");
            resSearch.Items.First().DiscountedPrice.Should().Be(90000m); // 100k - 10%
            resSearch.Items.First().HasPromotion.Should().BeTrue();
            resSearch.Items.First().Origin.Should().Be("New Zealand");
            resSearch.Items.First().Certification.Should().Be("GlobalGAP");
            resSearch.Items.First().BrixLevel.Should().Be("15");
            resSearch.Items.First().TotalAvailableStock.Should().Be(50);
            resSearch.Items.First().IsInStock.Should().BeTrue();

            resGroup.TotalRecords.Should().Be(2);
            resOrigin.TotalRecords.Should().Be(1);
            resCert.TotalRecords.Should().Be(1);
            resCert.Items.First().Code.Should().Be("PROD-CAM");

            resSortPrice.Items.First().Code.Should().Be("PROD-CAM"); // 40,000 < 90,000
        }
        #endregion

        #region TC02: LẤY CHI TIẾT SẢN PHẨM THEO SLUG (PRODUCT DETAIL PAGE)
        /// <summary>
        /// TC02: Lấy thông tin chi tiết một sản phẩm theo slug SEO, tổng hợp thuộc tính EAV, biến thể, bảng giá và khuyến mãi.
        /// </summary>
        [Fact]
        public async Task GetProductBySlugAsync_ShouldReturnCompleteProductDetail()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cat = new ProductCategory { Id = 1, Code = "CAT1", Name = "Trái cây sạch", Slug = "trai-cay-sach", IsActive = true };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var attrDef = new AttributeDefinition { Id = 1, Name = "Vùng trồng", DataType = "TEXT", IsActive = true };

            context.ProductCategories.Add(cat);
            context.UoMs.Add(uom);
            context.AttributeDefinitions.Add(attrDef);

            var product = new Product
            {
                Id = 10,
                Code = "PROD-XOAI",
                Name = "Xoài Cát Hòa Lộc Tiền Giang",
                Slug = "xoai-cat-hoa-loc-tien-giang",
                Description = "Xoài ngọt thơm đặc sản",
                CategoryId = 1,
                BaseUoMId = 1,
                IsActive = true
            };
            var variant = new ProductVariant
            {
                Id = 100,
                Code = "SKU-XOAI-1",
                Name = "Xoài Cát Hộp 1Kg",
                ProductId = 10,
                IsActive = true
            };
            var price = new ProductVariantPrice
            {
                Id = 1,
                VariantId = 100,
                UoMId = 1,
                Price = 80000m,
                IsDefault = true,
                IsActive = true
            };
            var attrVal = new ProductAttribute
            {
                Id = 1,
                VariantId = 100,
                AttributeDefinitionId = 1,
                AttributeValue = "Tiền Giang",
                IsActive = true
            };

            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            context.ProductAttributes.Add(attrVal);
            await context.SaveChangesAsync();

            var service = new ShopProductService(context, _mapper);

            // Act
            var valid = await service.GetProductBySlugAsync("xoai-cat-hoa-loc-tien-giang");
            var invalid = await service.GetProductBySlugAsync("khong-ton-tai");

            // Assert
            valid.Should().NotBeNull();
            valid!.Code.Should().Be("PROD-XOAI");
            valid.CategoryName.Should().Be("Trái cây sạch");
            valid.Attributes.Should().ContainKey("Vùng trồng");
            valid.Attributes["Vùng trồng"].Should().Be("Tiền Giang");
            valid.Variants.Should().HaveCount(1);
            valid.Variants.First().Prices.First().Price.Should().Be(80000m);

            invalid.Should().BeNull();
        }
        #endregion

        #region TC03: LẤY CÂY DANH MỤC CHO MENU HEADER (CATEGORY TREE)
        /// <summary>
        /// TC03: Lấy cấu trúc cây phân cấp Nhóm ngành hàng -> Danh mục con (kèm đếm số lượng sản phẩm).
        /// </summary>
        [Fact]
        public async Task GetCategoryTreeAsync_ShouldReturnTreeWithProductCount()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var group = new ProductCategoryGroup { Id = 1, Code = "FOOD", Name = "Thực phẩm", Slug = "thuc-pham", IsActive = true };
            var cat1 = new ProductCategory { Id = 1, Code = "VEG", Name = "Rau xanh", Slug = "rau-xanh", CategoryGroupId = 1, IsActive = true };
            var cat2 = new ProductCategory { Id = 2, Code = "FRUIT", Name = "Trái cây", Slug = "trai-cay", CategoryGroupId = 1, IsActive = true };

            context.ProductCategoryGroups.Add(group);
            context.ProductCategories.AddRange(cat1, cat2);

            context.Products.AddRange(
                new Product { Id = 1, Code = "P1", Name = "Rau muống", CategoryId = 1, BaseUoMId = 1, IsActive = true },
                new Product { Id = 2, Code = "P2", Name = "Rau cải", CategoryId = 1, BaseUoMId = 1, IsActive = true },
                new Product { Id = 3, Code = "P3", Name = "Táo", CategoryId = 2, BaseUoMId = 1, IsActive = true }
            );
            await context.SaveChangesAsync();

            var service = new ShopProductService(context, _mapper);

            // Act
            var tree = await service.GetCategoryTreeAsync();

            // Assert
            tree.Should().HaveCount(1);
            var groupItem = tree.First();
            groupItem.GroupName.Should().Be("Thực phẩm");
            groupItem.Categories.Should().HaveCount(2);

            var vegCat = groupItem.Categories.First(c => c.CategorySlug == "rau-xanh");
            vegCat.ProductCount.Should().Be(2);

            var fruitCat = groupItem.Categories.First(c => c.CategorySlug == "trai-cay");
            fruitCat.ProductCount.Should().Be(1);
        }
        #endregion

        #region TC04: LẤY SẢN PHẨM NỔI BẬT & HÀNG MỚI VỀ
        /// <summary>
        /// TC04: Lấy danh sách sản phẩm nổi bật (giảm giá nhiều nhất) và sản phẩm mới về (New Arrivals).
        /// </summary>
        [Fact]
        public async Task GetFeaturedAndNewArrivals_ShouldReturnTopItems()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kg" });

            for (int i = 1; i <= 10; i++)
            {
                var prod = new Product { Id = i, Code = $"P{i}", Name = $"Sản phẩm {i}", BaseUoMId = 1, IsActive = true };
                var variant = new ProductVariant { Id = i * 10, Code = $"V{i}", Name = $"Var {i}", ProductId = i, IsActive = true };
                var price = new ProductVariantPrice { Id = i * 10, VariantId = i * 10, UoMId = 1, Price = 10000m * i, IsDefault = true, IsActive = true };

                context.Products.Add(prod);
                context.ProductVariants.Add(variant);
                context.ProductVariantPrices.Add(price);
            }
            await context.SaveChangesAsync();

            var service = new ShopProductService(context, _mapper);

            // Act
            var featured = await service.GetFeaturedProductsAsync(4);
            var newArrivals = await service.GetNewArrivalsAsync(4);

            // Assert
            featured.Should().HaveCount(4);
            newArrivals.Should().HaveCount(4);
        }
        #endregion

        #region TC05: LẤY CHIẾN DỊCH KHUYẾN MÃI & CHI TIẾT THEO SLUG
        /// <summary>
        /// TC05: Lấy danh sách khuyến mãi đang diễn ra và chi tiết khuyến mãi kèm danh sách sản phẩm áp dụng.
        /// </summary>
        [Fact]
        public async Task GetActivePromotionsAndBySlug_ShouldReturnPromotions()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var promoActive = new PromotionCampaign
            {
                Id = 1,
                Name = "Đại Tiệc Trái Cây",
                Slug = "dai-tiec-trai-cay",
                Description = "Giảm giá toàn bộ trái cây nhiệt đới",
                IsPercentage = true,
                DiscountValue = 15,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                IsActive = true
            };

            var promoExpired = new PromotionCampaign
            {
                Id = 2,
                Name = "Khuyến mãi đã hết hạn",
                StartDate = now.AddDays(-10),
                EndDate = now.AddDays(-1),
                IsActive = true
            };

            context.PromotionCampaigns.AddRange(promoActive, promoExpired);

            var cat = new ProductCategory { Id = 1, Code = "CAT1", Name = "Trái cây", Slug = "trai-cay", IsActive = true };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            context.ProductCategories.Add(cat);
            context.UoMs.Add(uom);

            var product = new Product { Id = 1, Code = "P1", Name = "Xoài", CategoryId = 1, BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "V1", Name = "Xoài Túi", ProductId = 1, Product = product, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 50000m, IsDefault = true, IsActive = true };

            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            context.PromotionVariants.Add(new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 10, Variant = variant });
            await context.SaveChangesAsync();

            var service = new ShopProductService(context, _mapper);

            // Act
            var activePromos = await service.GetActivePromotionsAsync();
            var promoDetail = await service.GetPromotionBySlugAsync("dai-tiec-trai-cay");
            var notFoundPromo = await service.GetPromotionBySlugAsync("khong-ton-tai");

            // Assert
            activePromos.Should().HaveCount(1);
            activePromos.First().Slug.Should().Be("dai-tiec-trai-cay");

            promoDetail.Should().NotBeNull();
            promoDetail!.Name.Should().Be("Đại Tiệc Trái Cây");
            promoDetail.Products.Should().HaveCount(1);
            promoDetail.Products.First().Code.Should().Be("P1");

            notFoundPromo.Should().BeNull();
        }
        #endregion

        #region TC06: LẤY BỘ LỌC ĐỘNG VÙNG TRỒNG (ORIGINS) VÀ TIÊU CHUẨN CHỨNG NHẬN (CERTIFICATIONS)
        /// <summary>
        /// TC06: Trích xuất danh sách xuất xứ vùng trồng và tiêu chuẩn chứng nhận duy nhất từ các thuộc tính EAV.
        /// </summary>
        [Fact]
        public async Task GetAvailableOriginsAndCertifications_ShouldExtractDistinctAttributes()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var attrOrigin = new AttributeDefinition { Id = 1, Name = "Xuất xứ vùng trồng", DataType = "TEXT", IsActive = true };
            var attrCert = new AttributeDefinition { Id = 2, Name = "Tiêu chuẩn chứng nhận", DataType = "TEXT", IsActive = true };

            context.AttributeDefinitions.AddRange(attrOrigin, attrCert);

            context.ProductAttributes.AddRange(
                new ProductAttribute { Id = 1, VariantId = 1, AttributeDefinitionId = 1, AttributeValue = "Đà Lạt" },
                new ProductAttribute { Id = 2, VariantId = 2, AttributeDefinitionId = 1, AttributeValue = "Mộc Châu" },
                new ProductAttribute { Id = 3, VariantId = 3, AttributeDefinitionId = 1, AttributeValue = "Đà Lạt" }, // Trùng
                new ProductAttribute { Id = 4, VariantId = 1, AttributeDefinitionId = 2, AttributeValue = "VietGAP" },
                new ProductAttribute { Id = 5, VariantId = 2, AttributeDefinitionId = 2, AttributeValue = "GlobalGAP" }
            );
            await context.SaveChangesAsync();

            var service = new ShopProductService(context, _mapper);

            // Act
            var origins = await service.GetAvailableOriginsAsync();
            var certs = await service.GetAvailableCertificationsAsync();

            // Assert
            origins.Should().HaveCount(2);
            origins.Should().Contain(new[] { "Đà Lạt", "Mộc Châu" });

            certs.Should().HaveCount(2);
            certs.Should().Contain(new[] { "VietGAP", "GlobalGAP" });
        }
        #endregion
    }
}
