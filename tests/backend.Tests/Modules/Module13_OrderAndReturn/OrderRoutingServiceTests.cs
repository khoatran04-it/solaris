using AutoMapper;
using backend.Data;
using backend.DTOs.OrderDTOs;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Services.Interfaces;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module13_OrderAndReturn
{
    /// <summary>
    /// ============================================================================
    /// MODULE 13: SALES ORDERS & CUSTOMER RETURNS
    /// UNIT TEST: OrderRoutingService & Hyperlocal Single-Hub Fulfillment
    /// ============================================================================
    /// </summary>
    public class OrderRoutingServiceTests
    {
        private readonly IMapper _mapper;

        public OrderRoutingServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        private static IOrderRoutingService CreateRoutingService(SolarisDbContext context)
        {
            return new OrderRoutingService(context, new DistanceService());
        }

        #region TC01: SINGLE-HUB POLICY — KHÔNG XÉ LẺ ĐƠN HÀNG, CHỌN KHO XA HƠN ĐỦ 100% HÀNG

        [Fact]
        public async Task DetermineOptimalWarehouseAsync_WhenNearestWarehouseLacksItem_SelectsFurtherWarehouseWithFullStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            // Khách hàng ở Quận 1 (10.7750, 106.7000)
            double custLat = 10.7750;
            double custLng = 106.7000;

            // Kho 1 (Quận 1): Cách khách hàng ~0.2 km (Rất gần)
            var whAddress1 = new WarehouseAddress
            {
                Id = 1,
                Province = "TP.HCM",
                District = "Quận 1",
                Ward = "Bến Nghé",
                StreetAddress = "10 Lê Lợi",
                Latitude = 10.7760,
                Longitude = 106.7010
            };
            var wh1 = new Warehouse
            {
                Id = 1,
                Code = "WH-Q1",
                Name = "Kho Solaris Quận 1",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 1,
                Address = whAddress1,
                IsActive = true
            };

            // Kho 2 (Quận 7): Cách khách hàng ~6.5 km (Xa hơn)
            var whAddress2 = new WarehouseAddress
            {
                Id = 2,
                Province = "TP.HCM",
                District = "Quận 7",
                Ward = "Tân Phong",
                StreetAddress = "500 Nguyễn Thị Thập",
                Latitude = 10.7300,
                Longitude = 106.7200
            };
            var wh2 = new Warehouse
            {
                Id = 2,
                Code = "WH-Q7",
                Name = "Kho Solaris Quận 7",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 2,
                Address = whAddress2,
                IsActive = true
            };

            context.WarehouseAddresses.AddRange(whAddress1, whAddress2);
            context.Warehouses.AddRange(wh1, wh2);

            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            context.UoMs.Add(uom);

            var cat = new ProductCategory { Id = 1, Code = "CAT-RAU", Name = "Rau Củ Tươi", RequiresColdChain = true, IsActive = true };
            context.ProductCategories.Add(cat);

            var prod1 = new Product { Id = 1, Code = "PROD-CAI", Name = "Cải Bó Xôi", CategoryId = 1, BaseUoMId = 1, IsActive = true };
            var var1 = new ProductVariant { Id = 101, Code = "SKU-CAI", Name = "Cải Bó Xôi 500g", ProductId = 1, IsActive = true };

            var prod2 = new Product { Id = 2, Code = "PROD-NAM", Name = "Nấm Đùi Gà", CategoryId = 1, BaseUoMId = 1, IsActive = true };
            var var2 = new ProductVariant { Id = 102, Code = "SKU-NAM", Name = "Nấm Đùi Gà 300g", ProductId = 2, IsActive = true };

            context.Products.AddRange(prod1, prod2);
            context.ProductVariants.AddRange(var1, var2);

            // Kho 1 (Quận 1) chỉ có Cải Bó Xôi (10kg), THIẾU Nấm Đùi Gà (0kg)
            var batch1 = new ProductBatch { Id = 1, BatchCode = "LOT-CAI-01", VariantId = 101, ExpiryDate = now.AddDays(10) };
            var invWh1Var1 = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 101, BatchId = 1, QuantityAvailable = 10, QuantityReserved = 0 };

            // Kho 2 (Quận 7) CÓ ĐỦ CẢ 2 MẶT HÀNG: Cải Bó Xôi (10kg) VÀ Nấm Đùi Gà (10kg)
            var batch2 = new ProductBatch { Id = 2, BatchCode = "LOT-NAM-02", VariantId = 102, ExpiryDate = now.AddDays(10) };
            var invWh2Var1 = new WarehouseInventory { Id = 2, WarehouseId = 2, VariantId = 101, BatchId = 1, QuantityAvailable = 10, QuantityReserved = 0 };
            var invWh2Var2 = new WarehouseInventory { Id = 3, WarehouseId = 2, VariantId = 102, BatchId = 2, QuantityAvailable = 10, QuantityReserved = 0 };

            context.ProductBatches.AddRange(batch1, batch2);
            context.WarehouseInventories.AddRange(invWh1Var1, invWh2Var1, invWh2Var2);
            await context.SaveChangesAsync();

            var routingService = CreateRoutingService(context);

            // Giỏ hàng của khách yêu cầu cả 2 món: 2 Cải Bó Xôi + 2 Nấm Đùi Gà
            var orderItems = new List<OrderDetailCreateDto>
            {
                new() { VariantId = 101, Quantity = 2 },
                new() { VariantId = 102, Quantity = 2 }
            };

            // Act
            var result = await routingService.DetermineOptimalWarehouseAsync(
                customerAddressId: null,
                items: orderItems,
                directLat: custLat,
                directLng: custLng,
                province: "TP.HCM",
                district: "Quận 1");

            // Assert: Single-Hub Policy bắt buộc chọn Kho 2 (Quận 7) để gom 100% đơn hàng,
            // không xé lẻ đơn sang Kho 1 dù Kho 1 gần hơn!
            result.Should().NotBeNull();
            result.OptimalWarehouseId.Should().Be(2, "Kho 2 (Quận 7) có đủ 100% cả 2 mặt hàng trong giỏ");
            result.WarehouseName.Should().Be("Kho Solaris Quận 7");
            result.IsFullyStocked.Should().BeTrue();
            result.MissingItems.Should().BeEmpty();
        }

        #endregion

        #region TC02: SINGLE-HUB POLICY — KHI KHÔNG CÓ KHO NÀO ĐỦ 100%, FALLBACK VỀ KHO GẦN NHẤT BÁO THIẾU

        [Fact]
        public async Task DetermineOptimalWarehouseAsync_WhenNoWarehouseCanFulfillAll_SelectsNearestWarehouseWithMissingItems()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            double custLat = 10.7750;
            double custLng = 106.7000;

            var whAddress1 = new WarehouseAddress
            {
                Id = 1,
                Province = "TP.HCM",
                District = "Quận 1",
                Ward = "Bến Nghé",
                StreetAddress = "10 Lê Lợi",
                Latitude = 10.7760,
                Longitude = 106.7010
            };
            var wh1 = new Warehouse
            {
                Id = 1,
                Code = "WH-Q1",
                Name = "Kho Solaris Quận 1",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 1,
                Address = whAddress1,
                IsActive = true
            };

            var whAddress2 = new WarehouseAddress
            {
                Id = 2,
                Province = "TP.HCM",
                District = "Quận 7",
                Ward = "Tân Phong",
                StreetAddress = "500 Nguyễn Thị Thập",
                Latitude = 10.7300,
                Longitude = 106.7200
            };
            var wh2 = new Warehouse
            {
                Id = 2,
                Code = "WH-Q7",
                Name = "Kho Solaris Quận 7",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 2,
                Address = whAddress2,
                IsActive = true
            };

            context.WarehouseAddresses.AddRange(whAddress1, whAddress2);
            context.Warehouses.AddRange(wh1, wh2);

            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            context.UoMs.Add(uom);

            var prod = new Product { Id = 1, Code = "PROD-TAO", Name = "Táo Envy", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 201, Code = "SKU-TAO", Name = "Táo Envy New Zealand", ProductId = 1, IsActive = true };
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);

            // Cả 2 kho đều chỉ có 2 quả táo, trong khi khách đặt 10 quả
            var batch = new ProductBatch { Id = 1, BatchCode = "LOT-TAO-01", VariantId = 201, ExpiryDate = now.AddDays(15) };
            var invWh1 = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 201, BatchId = 1, QuantityAvailable = 2, QuantityReserved = 0 };
            var invWh2 = new WarehouseInventory { Id = 2, WarehouseId = 2, VariantId = 201, BatchId = 1, QuantityAvailable = 2, QuantityReserved = 0 };

            context.ProductBatches.Add(batch);
            context.WarehouseInventories.AddRange(invWh1, invWh2);
            await context.SaveChangesAsync();

            var routingService = CreateRoutingService(context);

            var orderItems = new List<OrderDetailCreateDto>
            {
                new() { VariantId = 201, Quantity = 10 }
            };

            // Act
            var result = await routingService.DetermineOptimalWarehouseAsync(
                customerAddressId: null,
                items: orderItems,
                directLat: custLat,
                directLng: custLng,
                province: "TP.HCM",
                district: "Quận 1");

            // Assert: Fallback về Kho 1 (gần nhất) và trả về IsFullyStocked = false kèm MissingItems
            result.Should().NotBeNull();
            result.OptimalWarehouseId.Should().Be(1, "Phải fallback về kho gần nhất khi không kho nào đủ hàng");
            result.IsFullyStocked.Should().BeFalse();
            result.MissingItems.Should().ContainSingle(m => m.VariantId == 201 && m.AvailableQuantity == 2 && m.RequestedQuantity == 10);
        }

        #endregion

        #region TC03: LOẠI BỎ LÔ HẾT HẠN SỬ DỤNG (EXPIRED BATCHES EXCLUDED)

        [Fact]
        public async Task DetermineOptimalWarehouseAsync_WhenStockIsExpired_ExcludesFromAvailableStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            double custLat = 10.7750;
            double custLng = 106.7000;

            // Kho 1 (gần hơn) có hàng nhưng LÔ ĐÃ HẾT HẠN
            var whAddress1 = new WarehouseAddress { Id = 1, Province = "TP.HCM", District = "Quận 1", Ward = "Bến Nghé", StreetAddress = "10 Lê Lợi", Latitude = 10.7760, Longitude = 106.7010 };
            var wh1 = new Warehouse { Id = 1, Code = "WH-Q1", Name = "Kho Q1", WarehouseType = WarehouseTypeConstants.Retail, AddressId = 1, Address = whAddress1, IsActive = true };

            // Kho 2 (xa hơn) có hàng VÀ LÔ CÒN HẠN
            var whAddress2 = new WarehouseAddress { Id = 2, Province = "TP.HCM", District = "Quận 7", Ward = "Tân Phong", StreetAddress = "500 Nguyễn Thị Thập", Latitude = 10.7300, Longitude = 106.7200 };
            var wh2 = new Warehouse { Id = 2, Code = "WH-Q7", Name = "Kho Q7", WarehouseType = WarehouseTypeConstants.Retail, AddressId = 2, Address = whAddress2, IsActive = true };

            context.WarehouseAddresses.AddRange(whAddress1, whAddress2);
            context.Warehouses.AddRange(wh1, wh2);

            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var prod = new Product { Id = 1, Code = "PROD-CAM", Name = "Cam Sành", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 301, Code = "SKU-CAM", Name = "Cam Sành 1kg", ProductId = 1, IsActive = true };
            context.UoMs.Add(uom);
            context.Products.Add(prod);
            context.ProductVariants.Add(variant);

            // Kho 1: Lô đã hết hạn 2 ngày trước (ExpiryDate < now)
            var batchExpired = new ProductBatch { Id = 1, BatchCode = "LOT-EXPIRED", VariantId = 301, ExpiryDate = now.AddDays(-2) };
            var invWh1 = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 301, BatchId = 1, QuantityAvailable = 50, QuantityReserved = 0 };

            // Kho 2: Lô còn hạn 15 ngày
            var batchValid = new ProductBatch { Id = 2, BatchCode = "LOT-VALID", VariantId = 301, ExpiryDate = now.AddDays(15) };
            var invWh2 = new WarehouseInventory { Id = 2, WarehouseId = 2, VariantId = 301, BatchId = 2, QuantityAvailable = 50, QuantityReserved = 0 };

            context.ProductBatches.AddRange(batchExpired, batchValid);
            context.WarehouseInventories.AddRange(invWh1, invWh2);
            await context.SaveChangesAsync();

            var routingService = CreateRoutingService(context);

            var orderItems = new List<OrderDetailCreateDto>
            {
                new() { VariantId = 301, Quantity = 5 }
            };

            // Act
            var result = await routingService.DetermineOptimalWarehouseAsync(
                customerAddressId: null,
                items: orderItems,
                directLat: custLat,
                directLng: custLng,
                province: "TP.HCM",
                district: "Quận 1");

            // Assert: Kho 1 bị coi là hết hàng vì lô đã hết hạn -> Chọn Kho 2
            result.Should().NotBeNull();
            result.OptimalWarehouseId.Should().Be(2, "Kho 1 có tồn kho nhưng lô đã hết hạn sử dụng");
            result.IsFullyStocked.Should().BeTrue();
        }

        #endregion

        #region TC04: COLD-CHAIN GEOFENCING — CHẶN ĐƠN HÀNG TƯƠI SỐNG VƯỢT QUÁ BÁN KÍNH 15KM

        [Fact]
        public async Task CheckoutAsync_WhenColdChainProductExceeds15Km_ThrowsException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser { Id = 1, CitizenId = "001200000001", Username = "system", FullName = "System", Email = "sys@solaris.vn", PhoneNumber = "0900000001", PasswordHash = "hash", IsActive = true };
            context.IAUsers.Add(user);

            var tier = new CustomerTier { Id = 1, Code = "TIER-STANDARD", Name = "Standard", DiscountPercent = 0, IsActive = true };
            var customer = new Customer { Id = 1, Code = "CUST-01", Name = "Khách Huyện Củ Chi", PhoneNumber = "0911222333", CustomerTierId = 1, IsActive = true };
            context.CustomerTiers.Add(tier);
            context.Customers.Add(customer);

            // Kho Bán Lẻ duy nhất ở Quận 1 (10.7760, 106.7010) với MaxColdChainRadiusKm = 15.0 km
            var whAddress = new WarehouseAddress { Id = 1, Province = "TP.HCM", District = "Quận 1", Ward = "Bến Nghé", StreetAddress = "10 Lê Lợi", Latitude = 10.7760, Longitude = 106.7010 };
            var warehouse = new Warehouse
            {
                Id = 1,
                Code = "WH-Q1",
                Name = "Kho Quận 1",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 1,
                Address = whAddress,
                MaxColdChainRadiusKm = 15.0,
                IsActive = true
            };
            context.WarehouseAddresses.Add(whAddress);
            context.Warehouses.Add(warehouse);

            // Sản phẩm thuộc Danh mục "Thịt Cá Tươi Sống" với RequiresColdChain = true
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var coldCat = new ProductCategory { Id = 1, Code = "CAT-COLD", Name = "Thịt Cá Tươi Sống", RequiresColdChain = true, IsActive = true };
            var product = new Product { Id = 1, Code = "PROD-CA-HOI", Name = "Cá Hồi Na Uy Tươi", CategoryId = 1, Category = coldCat, BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 401, Code = "SKU-CA-HOI", Name = "Cá Hồi Fillet 500g", ProductId = 1, Product = product, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 401, UoMId = 1, Price = 250000m, IsActive = true, IsDefault = true };

            context.UoMs.Add(uom);
            context.ProductCategories.Add(coldCat);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);

            var batch = new ProductBatch { Id = 1, BatchCode = "LOT-FISH-01", VariantId = 401, ExpiryDate = now.AddDays(5) };
            var inv = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 401, BatchId = 1, QuantityAvailable = 20, QuantityReserved = 0 };
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inv);

            // Giỏ hàng của khách hàng
            var cart = new ShoppingCart { Id = 1, CustomerId = 1, CreatedAt = now, UpdatedAt = now };
            var cartItem = new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 401, UoMId = 1, Quantity = 1, AddedAt = now, UpdatedAt = now };
            cart.Items.Add(cartItem);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var routingService = CreateRoutingService(context);
            var shopOrderService = new ShopOrderService(context, _mapper, routingService);

            // Khách đặt giao đến Huyện Củ Chi (Lat 10.9700, Lng 106.4900) -> Khoảng cách đường sá ~30 km (> 15 km)
            var checkoutDto = new ShopCheckoutRequestDto
            {
                ReceiverName = "Khách Huyện Củ Chi",
                ReceiverPhone = "0911222333",
                Province = "TP.HCM",
                District = "Huyện Củ Chi",
                Ward = "Thị trấn Củ Chi",
                StreetAddress = "123 Tỉnh Lộ 8",
                Latitude = 10.9700,
                Longitude = 106.4900,
                PaymentMethod = PaymentMethod.COD,
                ShippingFee = 45000m
            };

            // Act & Assert: Phải chặn đứng bằng Exception chứa thông tin chuỗi lạnh
            var act = () => shopOrderService.CheckoutAsync(1, checkoutDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*chuỗi lạnh*vượt quá bán kính bảo quản tối đa*15*");
        }

        #endregion

        #region TC05: AMBIENT PRODUCTS EXEMPTION — SẢN PHẨM KHÔ MIỄN TRỪ RÀO CHẮN 15KM

        [Fact]
        public async Task CheckoutAsync_WhenAmbientProducts_ExemptFromColdChainGeofence_Succeeds()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser { Id = 1, CitizenId = "001200000001", Username = "system", FullName = "System", Email = "sys@solaris.vn", PhoneNumber = "0900000001", PasswordHash = "hash", IsActive = true };
            context.IAUsers.Add(user);

            var tier = new CustomerTier { Id = 1, Code = "TIER-STANDARD", Name = "Standard", DiscountPercent = 0, IsActive = true };
            var customer = new Customer { Id = 2, Code = "CUST-02", Name = "Khách Huyện Cần Giờ", PhoneNumber = "0911444555", CustomerTierId = 1, IsActive = true };
            context.CustomerTiers.Add(tier);
            context.Customers.Add(customer);

            var whAddress = new WarehouseAddress { Id = 1, Province = "TP.HCM", District = "Quận 1", Ward = "Bến Nghé", StreetAddress = "10 Lê Lợi", Latitude = 10.7760, Longitude = 106.7010 };
            var warehouse = new Warehouse
            {
                Id = 1,
                Code = "WH-Q1",
                Name = "Kho Quận 1",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 1,
                Address = whAddress,
                MaxColdChainRadiusKm = 15.0,
                IsActive = true
            };
            context.WarehouseAddresses.Add(whAddress);
            context.Warehouses.Add(warehouse);

            // Sản phẩm thuộc Danh mục "Gạo & Nông Sản Khô" với RequiresColdChain = false
            var uom = new UoM { Id = 1, Code = "BAG", Name = "Bao 5kg", IsActive = true };
            var ambientCat = new ProductCategory { Id = 2, Code = "CAT-DRY", Name = "Gạo & Nông Sản Khô", RequiresColdChain = false, IsActive = true };
            var product = new Product { Id = 2, Code = "PROD-GAO-ST25", Name = "Gạo ST25 Ông Cua", CategoryId = 2, Category = ambientCat, BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 501, Code = "SKU-GAO-ST25", Name = "Gạo ST25 Túi 5kg", ProductId = 2, Product = product, IsActive = true };
            var price = new ProductVariantPrice { Id = 2, VariantId = 501, UoMId = 1, Price = 180000m, IsActive = true, IsDefault = true };

            context.UoMs.Add(uom);
            context.ProductCategories.Add(ambientCat);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);

            var batch = new ProductBatch { Id = 2, BatchCode = "LOT-RICE-01", VariantId = 501, ExpiryDate = now.AddDays(180) };
            var inv = new WarehouseInventory { Id = 2, WarehouseId = 1, VariantId = 501, BatchId = 2, QuantityAvailable = 50, QuantityReserved = 0 };
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inv);

            // Giỏ hàng của khách hàng chỉ có Gạo khô
            var cart = new ShoppingCart { Id = 2, CustomerId = 2, CreatedAt = now, UpdatedAt = now };
            var cartItem = new ShoppingCartItem { Id = 2, CartId = 2, VariantId = 501, UoMId = 1, Quantity = 2, AddedAt = now, UpdatedAt = now };
            cart.Items.Add(cartItem);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var routingService = CreateRoutingService(context);
            var shopOrderService = new ShopOrderService(context, _mapper, routingService);

            // Khách đặt giao đến Cần Giờ (Lat 10.4100, Lng 106.9600) -> Khoảng cách ~50km (> 15km)
            var checkoutDto = new ShopCheckoutRequestDto
            {
                ReceiverName = "Khách Huyện Cần Giờ",
                ReceiverPhone = "0911444555",
                Province = "TP.HCM",
                District = "Huyện Cần Giờ",
                Ward = "Thị trấn Cần Thạnh",
                StreetAddress = "88 Đào Cử",
                Latitude = 10.4100,
                Longitude = 106.9600,
                PaymentMethod = PaymentMethod.COD,
                ShippingFee = 50000m
            };

            // Act
            var orderResult = await shopOrderService.CheckoutAsync(2, checkoutDto);

            // Assert: Đơn hàng hoàn tất bình thường vì hàng khô miễn trừ rào chắn chuỗi lạnh!
            orderResult.Should().NotBeNull();
            orderResult.Status.Should().Be(OrderStatus.Pending);
            orderResult.TotalAmount.Should().Be(180000m * 2 + 50000m);
        }

        #endregion
    }
}
