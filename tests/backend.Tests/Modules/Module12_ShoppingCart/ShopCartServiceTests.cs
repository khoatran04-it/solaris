using AutoMapper;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module12_ShoppingCart
{
    /// <summary>
    /// ============================================================================
    /// MODULE 12: SHOPPING CART (GIỎ HÀNG MUA SẮM)
    /// UNIT TEST: ShopCartService (Quản Lý Giỏ Hàng, Tồn Kho Thời Gian Thực & Đồng Bộ Khách Vãng Lai)
    /// ============================================================================
    /// </summary>
    public class ShopCartServiceTests
    {
        private readonly IMapper _mapper;

        public ShopCartServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: KHỞI TẠO GIỎ HÀNG MỚI NẾU CHƯA CÓ TRONG HỆ THỐNG
        /// <summary>
        /// TC01: Kiểm tra khi khách hàng lần đầu truy cập giỏ hàng, hệ thống tự động khởi tạo 
        /// thực thể ShoppingCart mới và trả về DTO giỏ hàng rỗng với các chỉ số tài chính bằng 0.
        /// </summary>
        [Fact]
        public async Task GetCartAsync_ShouldCreateNewCartIfNotExistAndReturnEmptyCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new ShopCartService(context, _mapper);
            int customerId = 100;

            // Act
            var result = await service.GetCartAsync(customerId);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(0);
            result.SubTotal.Should().Be(0);
            result.TotalDiscount.Should().Be(0);
            result.EstimatedTotal.Should().Be(0);
            result.Items.Should().BeEmpty();

            var savedCart = await context.ShoppingCarts.FirstOrDefaultAsync(c => c.CustomerId == customerId);
            savedCart.Should().NotBeNull();
            savedCart!.CustomerId.Should().Be(customerId);
        }
        #endregion

        #region TC02: TÍNH TOÁN TỒN KHO KHẢ DỤNG THỜI GIAN THỰC & CỜ BÁO HẾT HÀNG
        /// <summary>
        /// TC02: Kiểm tra hàm GetCart tính toán đúng tồn kho khả dụng từ các Lô hàng còn hạn sử dụng,
        /// bỏ qua các Lô hàng đã hết hạn sử dụng, và bật cờ IsOutOfStock = true nếu khách chọn mua vượt tồn.
        /// </summary>
        [Fact]
        public async Task GetCartAsync_ShouldCalculateRealTimeAvailableStockAndOutOfStockFlag()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var uom = new UoM { Id = 1, Code = "HOP", Name = "Hộp 500g", IsActive = true };
            var prod = new Product { Id = 1, Code = "PROD-DAU", Name = "Dâu Tây Đà Lạt", Slug = "dau-tay-da-lat", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-DAU-500G", Name = "Dâu Tây Hộp 500g", ProductId = 1, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 120000m, IsActive = true, IsDefault = true };

            // Lô 1: Còn hạn (Hạn +30 ngày) -> Tồn kho 10 hộp
            var batchValid = new ProductBatch { Id = 1, BatchCode = "BATCH-VALID", VariantId = 10, ExpiryDate = now.AddDays(30) };
            var invValid = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, BatchId = 1, QuantityAvailable = 10 };

            // Lô 2: Đã hết hạn (Hạn -5 ngày) -> Tồn kho 20 hộp (Phải bị loại trừ)
            var batchExpired = new ProductBatch { Id = 2, BatchCode = "BATCH-EXPIRED", VariantId = 10, ExpiryDate = now.AddDays(-5) };
            var invExpired = new WarehouseInventory { Id = 2, WarehouseId = 1, VariantId = 10, BatchId = 2, QuantityAvailable = 20 };

            // Giỏ hàng của khách chứa 15 hộp (Khách chọn 15 > Tồn hợp lệ 10 -> IsOutOfStock = true)
            var cart = new ShoppingCart { Id = 1, CustomerId = 10, CreatedAt = now, UpdatedAt = now };
            var cartItem = new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 10, UoMId = 1, Quantity = 15, AddedAt = now, UpdatedAt = now };
            cart.Items.Add(cartItem);

            context.UoMs.Add(uom);
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            context.ProductBatches.AddRange(batchValid, batchExpired);
            context.WarehouseInventories.AddRange(invValid, invExpired);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act
            var result = await service.GetCartAsync(10);

            // Assert
            result.Items.Should().HaveCount(1);
            var item = result.Items[0];
            item.VariantId.Should().Be(10);
            item.VariantName.Should().Be("Dâu Tây Hộp 500g");
            item.ProductSlug.Should().Be("dau-tay-da-lat");
            item.AvailableStock.Should().Be(10); // Chỉ tính lô còn hạn
            item.IsOutOfStock.Should().BeTrue(); // 15 > 10
        }
        #endregion

        #region TC03: TÍNH TOÁN ĐƠN GIÁ THEO ĐVT, CHIẾT KHẤU KHUYẾN MÃI & TỔNG TIỀN
        /// <summary>
        /// TC03: Kiểm tra tính toán đơn giá theo ĐVT, tự động áp dụng chương trình khuyến mãi % tốt nhất,
        /// trích xuất thông tin xuất xứ EAV và tổng hợp SubTotal, TotalDiscount, EstimatedTotal.
        /// </summary>
        [Fact]
        public async Task GetCartAsync_ShouldApplyBestPromotionAndCalculateDiscountsAndTotals()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var uomKg = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var uomBox = new UoM { Id = 2, Code = "BOX", Name = "Hộp 500g", IsActive = true };
            var attrDef = new AttributeDefinition { Id = 10, Name = "Xuất xứ", DataType = "TEXT", IsActive = true };

            var prod = new Product { Id = 1, Code = "PROD-BO", Name = "Bơ Booth 7", Slug = "bo-booth-7", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-BO-KG", Name = "Bơ Booth 7 Loại 1", ProductId = 1, IsActive = true };

            // Giá 100,000 VND / Kg
            var priceKg = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 100000m, IsActive = true, IsDefault = true };
            var attrVal = new ProductAttribute { Id = 1, VariantId = 10, AttributeDefinitionId = 10, AttributeValue = "Đắk Lắk", IsActive = true };

            // Khuyến mãi giảm giá 20% đang hiệu lực
            var promo = new PromotionCampaign
            {
                Id = 1,
                Name = "Giảm giá 20% Bơ Đắk Lắk",
                IsPercentage = true,
                DiscountValue = 20,
                StartDate = now.AddDays(-2),
                EndDate = now.AddDays(5),
                IsActive = true
            };
            var promoVar = new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 10 };

            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-BO", VariantId = 10, ExpiryDate = now.AddDays(30) };
            var inv = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, BatchId = 1, QuantityAvailable = 50 };

            // Khách chọn mua 3 Kg Bơ
            var cart = new ShoppingCart { Id = 1, CustomerId = 20, CreatedAt = now, UpdatedAt = now };
            var cartItem = new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 10, UoMId = 1, Quantity = 3, AddedAt = now, UpdatedAt = now };
            cart.Items.Add(cartItem);

            context.UoMs.AddRange(uomKg, uomBox);
            context.AttributeDefinitions.Add(attrDef);
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(priceKg);
            context.ProductAttributes.Add(attrVal);
            context.PromotionCampaigns.Add(promo);
            context.PromotionVariants.Add(promoVar);
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inv);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act
            var result = await service.GetCartAsync(20);

            // Assert
            result.TotalItems.Should().Be(1);
            result.SubTotal.Should().Be(300000m); // 3 * 100,000
            result.TotalDiscount.Should().Be(60000m); // 3 * 20,000
            result.EstimatedTotal.Should().Be(240000m); // 300,000 - 60,000

            var item = result.Items[0];
            item.OriginalPrice.Should().Be(100000m);
            item.UnitPrice.Should().Be(80000m); // Giảm 20% = 80k
            item.DiscountAmount.Should().Be(20000m);
            item.TotalPrice.Should().Be(240000m);
            item.Origin.Should().Be("Đắk Lắk");
            item.AvailableStock.Should().Be(50);
            item.IsOutOfStock.Should().BeFalse();
        }
        #endregion

        #region TC04: THÊM SẢN PHẨM MỚI VÀO GIỎ HÀNG
        /// <summary>
        /// TC04: Kiểm tra thêm một mặt hàng mới (VariantId + UoMId) chưa từng có trong giỏ, 
        /// hệ thống tạo mới dòng ShoppingCartItem và lưu thời gian AddedAt / UpdatedAt.
        /// </summary>
        [Fact]
        public async Task AddItemAsync_ShouldAddNewItemWhenNotExistsInCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var prod = new Product { Id = 1, Code = "PROD-XOAI", Name = "Xoài Cát Hòa Lộc", Slug = "xoai-cat-hoa-loc", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-XOAI", Name = "Xoài Cát 1Kg", ProductId = 1, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 75000m, IsActive = true, IsDefault = true };

            context.UoMs.Add(uom);
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);
            var payload = new ShopCartAddDto { VariantId = 10, UoMId = 1, Quantity = 2 };

            // Act
            var result = await service.AddItemAsync(50, payload);

            // Assert
            result.TotalItems.Should().Be(1);
            result.SubTotal.Should().Be(150000m);
            result.Items.Should().HaveCount(1);
            result.Items[0].VariantId.Should().Be(10);
            result.Items[0].Quantity.Should().Be(2);

            var dbCart = await context.ShoppingCarts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == 50);
            dbCart!.Items.Should().HaveCount(1);
            dbCart.Items.First().Quantity.Should().Be(2);
        }
        #endregion

        #region TC05: CỘNG DỒN SỐ LƯỢNG KHI THÊM SẢN PHẨM & ĐVT ĐÃ CÓ SẴN TRONG GIỎ
        /// <summary>
        /// TC05: Kiểm tra khi thêm sản phẩm đã có trong giỏ với đúng ĐVT (UoMId), 
        /// hệ thống tự động cộng dồn số lượng thay vì tạo ra dòng ShoppingCartItem trùng lặp.
        /// </summary>
        [Fact]
        public async Task AddItemAsync_ShouldAccumulateQuantityWhenSameVariantAndUoMAlreadyInCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var prod = new Product { Id = 1, Code = "PROD-CAM", Name = "Cam Sành", Slug = "cam-sanh", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 20, Code = "SKU-CAM", Name = "Cam Sành 1Kg", ProductId = 1, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 20, UoMId = 1, Price = 40000m, IsActive = true, IsDefault = true };

            var cart = new ShoppingCart { Id = 1, CustomerId = 60, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            cart.Items.Add(new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 20, UoMId = 1, Quantity = 3, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            context.UoMs.Add(uom);
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);
            var payload = new ShopCartAddDto { VariantId = 20, UoMId = 1, Quantity = 4 };

            // Act: Thêm tiếp 4 Kg Cam vào giỏ
            var result = await service.AddItemAsync(60, payload);

            // Assert: Số lượng phải là 3 + 4 = 7 Kg, chỉ có 1 dòng mặt hàng
            result.TotalItems.Should().Be(1);
            result.Items[0].Quantity.Should().Be(7);
            result.SubTotal.Should().Be(280000m); // 7 * 40k

            var dbCart = await context.ShoppingCarts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == 60);
            dbCart!.Items.Should().HaveCount(1);
            dbCart.Items.First().Quantity.Should().Be(7);
        }
        #endregion

        #region TC06: SHIELD KIỂM SOÁT SỐ LƯỢNG THÊM MỚI (QUANTITY > 0)
        /// <summary>
        /// TC06: Kiểm tra lá chắn nghiệp vụ ngăn chặn thêm sản phẩm với số lượng &lt;= 0.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task AddItemAsync_ShouldThrowArgumentException_WhenQuantityIsZeroOrNegative(decimal invalidQty)
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new ShopCartService(context, _mapper);
            var payload = new ShopCartAddDto { VariantId = 1, UoMId = 1, Quantity = invalidQty };

            // Act
            Func<Task> act = async () => await service.AddItemAsync(1, payload);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Số lượng đặt mua phải lớn hơn 0.");
        }
        #endregion

        #region TC07: SHIELD KIỂM TRA SẢN PHẨM TỒN TẠI VÀ ĐANG KINH DOANH
        /// <summary>
        /// TC07: Kiểm tra ném KeyNotFoundException khi thêm sản phẩm không tồn tại, bị khóa (IsActive=false) hoặc đã xóa mềm.
        /// </summary>
        [Fact]
        public async Task AddItemAsync_ShouldThrowKeyNotFoundException_WhenVariantNotFoundOrInactive()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var variantInactive = new ProductVariant { Id = 10, Code = "SKU-INACTIVE", Name = "SP Ngừng Bán", IsActive = false };

            context.UoMs.Add(uom);
            context.ProductVariants.Add(variantInactive);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);
            var payload = new ShopCartAddDto { VariantId = 10, UoMId = 1, Quantity = 2 };

            // Act
            Func<Task> act = async () => await service.AddItemAsync(1, payload);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Không tìm thấy sản phẩm hoặc sản phẩm đã ngừng kinh doanh.");
        }
        #endregion

        #region TC08: SHIELD KIỂM TRA ĐƠN VỊ TÍNH TỒN TẠI VÀ ĐANG HOẠT ĐỘNG
        /// <summary>
        /// TC08: Kiểm tra ném KeyNotFoundException khi thêm sản phẩm với ĐVT không tồn tại hoặc ngừng sử dụng.
        /// </summary>
        [Fact]
        public async Task AddItemAsync_ShouldThrowKeyNotFoundException_WhenUoMNotFoundOrInactive()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var prod = new Product { Id = 1, Code = "PROD-ACT", Name = "SP Đang Bán", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-ACTIVE", Name = "SP Đang Bán", ProductId = 1, IsActive = true };
            var uomInactive = new UoM { Id = 99, Code = "OLD", Name = "ĐVT Cũ", IsActive = false };

            context.Products.Add(prod);
            context.ProductVariants.Add(variant);
            context.UoMs.Add(uomInactive);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);
            var payload = new ShopCartAddDto { VariantId = 10, UoMId = 99, Quantity = 1 };

            // Act
            Func<Task> act = async () => await service.AddItemAsync(1, payload);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Không tìm thấy đơn vị tính.");
        }
        #endregion

        #region TC09: CẬP NHẬT SỐ LƯỢNG DÒNG SẢN PHẨM HỢP LỆ
        /// <summary>
        /// TC09: Kiểm tra cập nhật số lượng mới (Quantity > 0) cho một dòng mặt hàng trong giỏ, 
        /// hệ thống cập nhật đúng số lượng và tính lại tổng tiền.
        /// </summary>
        [Fact]
        public async Task UpdateItemQuantityAsync_ShouldUpdateQuantity_WhenQuantityIsPositive()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var prod = new Product { Id = 1, Code = "PROD-CHUOI", Name = "Chuối Laba", Slug = "chuoi-laba", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 15, Code = "SKU-CHUOI", Name = "Chuối Laba 1Kg", ProductId = 1, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 15, UoMId = 1, Price = 30000m, IsActive = true, IsDefault = true };

            var cart = new ShoppingCart { Id = 1, CustomerId = 70, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var item = new ShoppingCartItem { Id = 101, CartId = 1, VariantId = 15, UoMId = 1, Quantity = 2, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            cart.Items.Add(item);

            context.UoMs.Add(uom);
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act: Cập nhật từ 2 lên 5 Kg
            var result = await service.UpdateItemQuantityAsync(70, 101, 5);

            // Assert
            result.Items.First().Quantity.Should().Be(5);
            result.SubTotal.Should().Be(150000m); // 5 * 30k

            var dbItem = await context.ShoppingCartItems.FindAsync(101);
            dbItem!.Quantity.Should().Be(5);
        }
        #endregion

        #region TC10: TỰ ĐỘNG XÓA DÒNG SẢN PHẨM KHI CẬP NHẬT SỐ LƯỢNG VỀ 0 HOẶC ÂM
        /// <summary>
        /// TC10: Kiểm tra nghiệp vụ khi khách hàng giảm số lượng về 0 hoặc âm tại trang giỏ hàng,
        /// hệ thống tự động xóa bỏ dòng sản phẩm đó ra khỏi giỏ.
        /// </summary>
        [Fact]
        public async Task UpdateItemQuantityAsync_ShouldRemoveItem_WhenQuantityIsZeroOrNegative()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cart = new ShoppingCart { Id = 1, CustomerId = 80, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var item = new ShoppingCartItem { Id = 102, CartId = 1, VariantId = 1, UoMId = 1, Quantity = 3, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            cart.Items.Add(item);

            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act: Cập nhật số lượng về 0
            var result = await service.UpdateItemQuantityAsync(80, 102, 0);

            // Assert
            result.TotalItems.Should().Be(0);
            result.Items.Should().BeEmpty();

            var dbItem = await context.ShoppingCartItems.FindAsync(102);
            dbItem.Should().BeNull();
        }
        #endregion

        #region TC11: NÉM LỖI KHI CẬP NHẬT DÒNG SẢN PHẨM KHÔNG TỒN TẠI
        /// <summary>
        /// TC11: Kiểm tra ném KeyNotFoundException khi cập nhật cartItemId không tồn tại trong giỏ của khách.
        /// </summary>
        [Fact]
        public async Task UpdateItemQuantityAsync_ShouldThrowKeyNotFoundException_WhenCartItemNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cart = new ShoppingCart { Id = 1, CustomerId = 90, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act
            Func<Task> act = async () => await service.UpdateItemQuantityAsync(90, 999, 5);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Không tìm thấy sản phẩm trong giỏ hàng.");
        }
        #endregion

        #region TC12: XÓA MỘT DÒNG SẢN PHẨM KHỎI GIỎ HÀNG
        /// <summary>
        /// TC12: Kiểm tra thao tác xóa hẳn 1 dòng mặt hàng (RemoveItemAsync) khỏi giỏ hàng.
        /// </summary>
        [Fact]
        public async Task RemoveItemAsync_ShouldRemoveItemFromCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cart = new ShoppingCart { Id = 1, CustomerId = 100, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var item1 = new ShoppingCartItem { Id = 10, CartId = 1, VariantId = 1, UoMId = 1, Quantity = 2, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var item2 = new ShoppingCartItem { Id = 11, CartId = 1, VariantId = 2, UoMId = 1, Quantity = 1, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            cart.Items.Add(item1);
            cart.Items.Add(item2);

            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act: Xóa dòng item 10
            var result = await service.RemoveItemAsync(100, 10);

            // Assert
            var dbItem = await context.ShoppingCartItems.FindAsync(10);
            dbItem.Should().BeNull();

            var remainingItem = await context.ShoppingCartItems.FindAsync(11);
            remainingItem.Should().NotBeNull();
        }
        #endregion

        #region TC13: LÀM SẠCH TOÀN BỘ GIỎ HÀNG
        /// <summary>
        /// TC13: Kiểm tra làm sạch giỏ hàng (ClearCartAsync) xóa toàn bộ các dòng mặt hàng sau khi thanh toán thành công.
        /// </summary>
        [Fact]
        public async Task ClearCartAsync_ShouldRemoveAllItemsFromCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var cart = new ShoppingCart { Id = 1, CustomerId = 110, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            cart.Items.Add(new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 1, UoMId = 1, Quantity = 2, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            cart.Items.Add(new ShoppingCartItem { Id = 2, CartId = 1, VariantId = 2, UoMId = 1, Quantity = 3, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Act
            var result = await service.ClearCartAsync(110);

            // Assert
            result.TotalItems.Should().Be(0);
            result.Items.Should().BeEmpty();

            var dbItems = await context.ShoppingCartItems.Where(i => i.CartId == 1).ToListAsync();
            dbItems.Should().BeEmpty();
        }
        #endregion

        #region TC14: ĐỒNG BỘ VÀ GỘP GIỎ HÀNG KHÁCH VÃNG LAI (GUEST CART SYNC)
        /// <summary>
        /// TC14: Kiểm tra đồng bộ giỏ hàng vãng lai (Guest Cart) khi khách đăng nhập:
        /// - Mặt hàng đã có trong giỏ Server -> Cộng dồn số lượng.
        /// - Mặt hàng chưa có trong giỏ Server -> Thêm mới vào giỏ.
        /// - Mặt hàng có số lượng &lt;= 0 hoặc không tồn tại -> Tự động bỏ qua an toàn.
        /// </summary>
        [Fact]
        public async Task SyncGuestCartAsync_ShouldMergeGuestItemsIntelligentlyIntoCustomerCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var prod1 = new Product { Id = 1, Code = "P1", Name = "Táo", Slug = "tao", BaseUoMId = 1, IsActive = true };
            var prod2 = new Product { Id = 2, Code = "P2", Name = "Lê", Slug = "le", BaseUoMId = 1, IsActive = true };

            var var1 = new ProductVariant { Id = 10, Code = "SKU-TAO", Name = "Táo 1Kg", ProductId = 1, IsActive = true };
            var var2 = new ProductVariant { Id = 20, Code = "SKU-LE", Name = "Lê 1Kg", ProductId = 2, IsActive = true };

            var price1 = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 50000m, IsActive = true, IsDefault = true };
            var price2 = new ProductVariantPrice { Id = 2, VariantId = 20, UoMId = 1, Price = 60000m, IsActive = true, IsDefault = true };

            // Giỏ hàng Server hiện có 2 Kg Táo
            var cart = new ShoppingCart { Id = 1, CustomerId = 120, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            cart.Items.Add(new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 10, UoMId = 1, Quantity = 2, AddedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            context.UoMs.Add(uom);
            context.Products.AddRange(prod1, prod2);
            context.ProductVariants.AddRange(var1, var2);
            context.ProductVariantPrices.AddRange(price1, price2);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopCartService(context, _mapper);

            // Giỏ hàng Guest từ LocalStorage gửi lên:
            // 1. Táo (Variant 10, UoM 1): 3 Kg (Cần cộng dồn 2 + 3 = 5 Kg)
            // 2. Lê (Variant 20, UoM 1): 4 Kg (Cần tạo mới dòng 4 Kg)
            // 3. Mặt hàng lỗi Variant 999: Bỏ qua
            // 4. Mặt hàng số lượng âm: Bỏ qua
            var guestPayload = new ShopSyncGuestCartDto
            {
                Items = new List<ShopGuestCartItemDto>
                {
                    new ShopGuestCartItemDto { VariantId = 10, UoMId = 1, Quantity = 3 },
                    new ShopGuestCartItemDto { VariantId = 20, UoMId = 1, Quantity = 4 },
                    new ShopGuestCartItemDto { VariantId = 999, UoMId = 1, Quantity = 5 },
                    new ShopGuestCartItemDto { VariantId = 10, UoMId = 1, Quantity = -1 }
                }
            };

            // Act
            var result = await service.SyncGuestCartAsync(120, guestPayload);

            // Assert
            result.TotalItems.Should().Be(2);

            var taoItem = result.Items.FirstOrDefault(i => i.VariantId == 10);
            taoItem.Should().NotBeNull();
            taoItem!.Quantity.Should().Be(5); // 2 + 3 = 5

            var leItem = result.Items.FirstOrDefault(i => i.VariantId == 20);
            leItem.Should().NotBeNull();
            leItem!.Quantity.Should().Be(4); // Tạo mới 4

            var dbCart = await context.ShoppingCarts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == 120);
            dbCart!.Items.Should().HaveCount(2);
        }
        #endregion

        #region TC15: ĐỒNG BỘ GIỎ HÀNG RỖNG TRẢ VỀ GIỎ HIỆN TẠI
        /// <summary>
        /// TC15: Kiểm tra khi danh sách guest items rỗng hoặc null, hệ thống trả về giỏ hàng hiện tại an toàn.
        /// </summary>
        [Fact]
        public async Task SyncGuestCartAsync_ShouldReturnExistingCart_WhenGuestItemsNullOrEmpty()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new ShopCartService(context, _mapper);
            var emptyPayload = new ShopSyncGuestCartDto { Items = new List<ShopGuestCartItemDto>() };

            // Act
            var result = await service.SyncGuestCartAsync(130, emptyPayload);

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(0);
        }
        #endregion
    }
}
