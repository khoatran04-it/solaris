using AutoMapper;
using backend.Data;
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
    /// 📦 MODULE 13: SALES ORDERS & CUSTOMER RETURNS
    /// 🧪 UNIT TEST: ShopOrderService (B2C Checkout, FEFO Lot Selection & Customer History)
    /// ============================================================================
    /// </summary>
    public class ShopOrderServiceTests
    {
        private readonly IMapper _mapper;

        public ShopOrderServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        private static IOrderRoutingService CreateRoutingService(SolarisDbContext context)
        {
            return new OrderRoutingService(context, new DistanceService());
        }

        #region TC01: B2C CHECKOUT TỪ GIỎ HÀNG, KHÓA GIỮ CHỖ TỒN KHO & XÓA GIỎ HÀNG
        /// <summary>
        /// TC01: Kiểm tra quy trình Checkout hoàn chỉnh:
        /// 1. Tự động tìm kho có lô hàng còn hạn sử dụng (FEFO).
        /// 2. Khóa giữ chỗ tồn kho (QuantityAvailable -= qty, QuantityReserved += qty).
        /// 3. Áp dụng giảm giá từ Chiến dịch khuyến mãi (PromotionCampaign).
        /// 4. Áp dụng giảm giá từ Hạng thành viên (CustomerTier).
        /// 5. Tạo đơn hàng trạng thái Confirmed và xóa sạch giỏ hàng của khách.
        /// </summary>
        [Fact]
        public async Task CheckoutAsync_ShouldCreateOrderReserveStockAndClearCart()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser
            {
                Id = 1,
                CitizenId = "001200000004",
                Username = "system",
                FullName = "System Worker",
                Email = "system@solaris.vn",
                PhoneNumber = "0904445555",
                PasswordHash = "hash",
                IsActive = true
            };
            var tier = new CustomerTier { Id = 1, Code = "TIER-GOLD", Name = "Gold VIP", DiscountPercent = 5, IsActive = true };
            var customer = new Customer
            {
                Id = 10,
                Code = "CUST-VIP",
                Name = "Khách Hàng VIP",
                PhoneNumber = "0909998888",
                CustomerTierId = 1,
                CustomerTier = tier,
                IsActive = true,
                Addresses = new List<CustomerAddress>
                {
                    new CustomerAddress
                    {
                        Id = 100,
                        CustomerId = 10,
                        ReceiverName = "Khách Hàng VIP",
                        Phone = "0909998888",
                        Province = "TP.HCM",
                        District = "Quận 1",
                        Ward = "Bến Nghé",
                        StreetAddress = "456 Nguyễn Huệ",
                        Latitude = 10.775,
                        Longitude = 106.700,
                        IsDefault = true
                    }
                }
            };

            var whAddress = new WarehouseAddress
            {
                Id = 1,
                Province = "TP.HCM",
                District = "Quận 1",
                Ward = "Bến Nghé",
                StreetAddress = "123 Lê Lợi",
                Latitude = 10.776,
                Longitude = 106.701
            };

            var warehouse = new Warehouse
            {
                Id = 1,
                Code = "WH-TONG",
                Name = "Kho Tổng TP.HCM",
                WarehouseType = "Kho Tổng",
                AddressId = 1,
                Address = whAddress,
                IsActive = true
            };

            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var product = new Product { Id = 1, Code = "PROD-XOAI", Name = "Xoài Cát Hòa Lộc", Slug = "xoai-cat-hoa-loc", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-XOAI", Name = "Xoài Cát Hộp 1kg", ProductId = 1, IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 100000m, IsActive = true, IsDefault = true };

            // Khuyến mãi giảm 10%
            var campaign = new PromotionCampaign
            {
                Id = 1,
                Name = "Mùa Hè Giảm Giá",
                Slug = "mua-he-giam-gia",
                IsPercentage = true,
                DiscountValue = 10m,
                StartDate = now.AddDays(-5),
                EndDate = now.AddDays(5),
                IsActive = true
            };
            var promoVariant = new PromotionVariant { Id = 1, PromotionCampaignId = 1, VariantId = 10, PromotionCampaign = campaign };

            // Lô hàng còn hạn
            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-XOAI-01", VariantId = 10, ExpiryDate = now.AddDays(30) };
            var inventory = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, BatchId = 1, QuantityAvailable = 20, QuantityReserved = 0 };

            // Giỏ hàng đang có 2kg
            var cart = new ShoppingCart { Id = 1, CustomerId = 10, CreatedAt = now, UpdatedAt = now };
            var cartItem = new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 10, UoMId = 1, Quantity = 2, AddedAt = now, UpdatedAt = now };
            cart.Items.Add(cartItem);

            context.IAUsers.Add(user);
            context.CustomerTiers.Add(tier);
            context.Customers.Add(customer);
            context.WarehouseAddresses.Add(whAddress);
            context.Warehouses.Add(warehouse);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            context.PromotionCampaigns.Add(campaign);
            context.PromotionVariants.Add(promoVariant);
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inventory);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            var checkoutRequest = new ShopCheckoutRequestDto
            {
                CustomerAddressId = 100,
                PaymentMethod = PaymentMethod.COD,
                ShippingFee = 20000m,
                Note = "Giao hàng cẩn thận"
            };

            // Act
            var orderResult = await service.CheckoutAsync(10, checkoutRequest);

            // Assert
            orderResult.Should().NotBeNull();
            orderResult.Status.Should().Be(OrderStatus.Confirmed);
            orderResult.ReceiverName.Should().Be("Khách Hàng VIP");
            orderResult.DeliveryAddress.Should().Be("456 Nguyễn Huệ, Bến Nghé, Quận 1, TP.HCM");
            orderResult.ShippingFee.Should().Be(20000m);

            // Kiểm tra tính tiền:
            // Đơn giá gốc: 100,000 * 2 = 200,000
            // Giảm giá SP 10%: 10,000 * 2 = 20,000
            // Chiết khấu VIP 5% trên SubTotal (200,000 * 5%): 10,000
            // Tổng giảm giá: 30,000
            // Tổng thanh toán: 200,000 - 30,000 + 20,000 (Ship) = 190,000
            orderResult.SubTotal.Should().Be(200000m);
            orderResult.DiscountAmount.Should().Be(30000m);
            orderResult.TotalAmount.Should().Be(190000m);

            // Kiểm tra tồn kho đã bị khóa giữ chỗ
            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityAvailable.Should().Be(18); // 20 - 2 = 18
            updatedInv.QuantityReserved.Should().Be(2);  // 0 + 2 = 2

            // Kiểm tra giỏ hàng đã được làm sạch sau checkout
            var remainingCartItems = await context.ShoppingCartItems.Where(i => i.CartId == 1).ToListAsync();
            remainingCartItems.Should().BeEmpty();
        }
        #endregion

        #region TC02: SHIELD CHECKOUT KHI GIỎ HÀNG RỖNG
        /// <summary>
        /// TC02: Kiểm tra khi khách hàng bấm checkout nhưng giỏ hàng rỗng sẽ ném InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task CheckoutAsync_WhenCartIsEmpty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var customer = new Customer { Id = 1, Code = "CUST", Name = "User", PhoneNumber = "0901112222", IsActive = true };
            var cart = new ShoppingCart { Id = 1, CustomerId = 1 };

            context.Customers.Add(customer);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            var checkoutRequest = new ShopCheckoutRequestDto { CustomerAddressId = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckoutAsync(1, checkoutRequest));
        }
        #endregion

        #region TC03: SHIELD CHECKOUT KHI TỒN KHO KHẢ DỤNG KHÔNG ĐỦ
        /// <summary>
        /// TC03: Kiểm tra khi số lượng trong giỏ vượt quá tồn kho khả dụng còn hạn của hệ thống sẽ ném InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task CheckoutAsync_WhenStockIsInsufficient_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST", Name = "User", PhoneNumber = "0901112222", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH", Name = "Kho", IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU", Name = "Sản phẩm", IsActive = true };
            var address = new CustomerAddress
            {
                Id = 1,
                CustomerId = 1,
                ReceiverName = "A",
                Phone = "123",
                Province = "HCM",
                District = "Q1",
                Ward = "Phường 1",
                StreetAddress = "123 Đường",
                IsDefault = true
            };
            customer.Addresses.Add(address);

            // Tồn kho chỉ có 1
            var inventory = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, QuantityAvailable = 1 };

            // Giỏ hàng muốn mua 5
            var cart = new ShoppingCart { Id = 1, CustomerId = 1 };
            cart.Items.Add(new ShoppingCartItem { Id = 1, CartId = 1, VariantId = 10, UoMId = 1, Quantity = 5 });

            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            context.ProductVariants.Add(variant);
            context.WarehouseInventories.Add(inventory);
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            var checkoutRequest = new ShopCheckoutRequestDto { CustomerAddressId = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckoutAsync(1, checkoutRequest));
        }
        #endregion

        #region TC04: TRUY VẤN LỊCH SỬ ĐƠN HÀNG CỦA KHÁCH HÀNG (CUSTOMER SCOPED)
        /// <summary>
        /// TC04: Kiểm tra hàm GetCustomerOrdersAsync chỉ trả về danh sách đơn hàng thuộc về customerId được truyền vào.
        /// </summary>
        [Fact]
        public async Task GetCustomerOrdersAsync_ShouldReturnPagedOrdersForCustomerOnly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var orderUser1 = new Order { Id = 1, OrderCode = "ORD-USER1", CustomerId = 10, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var orderUser2 = new Order { Id = 2, OrderCode = "ORD-USER2", CustomerId = 20, OrderDate = now, CreatedAt = now, UpdatedAt = now };

            context.Orders.AddRange(orderUser1, orderUser2);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            // Act
            var result = await service.GetCustomerOrdersAsync(10, pageIndex: 1, pageSize: 10);

            // Assert
            result.TotalRecords.Should().Be(1);
            result.Items.First().OrderCode.Should().Be("ORD-USER1");
        }
        #endregion

        #region TC05: TRUY VẤN CHI TIẾT ĐƠN HÀNG THEO MÃ CÔNG KHAI (ORDERCODE)
        /// <summary>
        /// TC05: Kiểm tra hàm GetOrderByCodeAsync trả về đúng đơn hàng theo orderCode và customerId.
        /// </summary>
        [Fact]
        public async Task GetOrderByCodeAsync_ShouldReturnOrderDetailsByPublicCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var product = new Product { Id = 1, Code = "PROD-1", Name = "Nho Mẫu Đơn", Slug = "nho-mau-don", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-NHO", Name = "Nho Mẫu Đơn Chùm 500g", ProductId = 1, IsActive = true };
            var uom = new UoM { Id = 1, Code = "CHUM", Name = "Chùm", IsActive = true };

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-PUBLIC-123",
                CustomerId = 10,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Unpaid,
                PaymentMethod = PaymentMethod.COD,
                SubTotal = 300000m,
                TotalAmount = 300000m,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, VariantId = 10, UoMId = 1, Quantity = 1, UnitPrice = 30000m, TotalPrice = 30000m }
                }
            };

            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.UoMs.Add(uom);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            // Act
            var result = await service.GetOrderByCodeAsync(10, "ORD-PUBLIC-123");

            // Assert
            result.Should().NotBeNull();
            result!.OrderCode.Should().Be("ORD-PUBLIC-123");
            result.StatusName.Should().Be("Đã xác nhận");
            result.PaymentMethodName.Should().Be("Thanh toán khi nhận hàng (COD)");
            result.Items.Should().HaveCount(1);
            result.Items.First().VariantName.Should().Be("Nho Mẫu Đơn Chùm 500g");
        }
        #endregion

        #region TC06: KHÁCH HÀNG TỰ HỦY ĐƠN HÀNG KHI CHƯA ĐÓNG GÓI/GIAO HÀNG
        /// <summary>
        /// TC06: Kiểm tra hàm CancelOrderAsync cho phép khách hủy đơn ở trạng thái Confirmed và tự động trả lại tồn kho.
        /// </summary>
        [Fact]
        public async Task CancelOrderAsync_WhenConfirmed_ShouldCancelAndUnreserveStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser
            {
                Id = 1,
                CitizenId = "001200000005",
                Username = "sys",
                FullName = "System",
                Email = "sys@solaris.vn",
                PhoneNumber = "0905556666",
                PasswordHash = "hash",
                IsActive = true
            };
            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-USER-CANCEL",
                CustomerId = 10,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, VariantId = 10, UoMId = 1, Quantity = 3 }
                }
            };

            var inventory = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 10,
                BatchId = 1,
                QuantityAvailable = 7,
                QuantityReserved = 3
            };

            context.IAUsers.Add(user);
            context.Orders.Add(order);
            context.WarehouseInventories.Add(inventory);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            // Act
            bool result = await service.CancelOrderAsync(10, "ORD-USER-CANCEL", "Tôi muốn đổi địa chỉ giao hàng");

            // Assert
            result.Should().BeTrue();

            var updatedOrder = await context.Orders.FindAsync(1);
            updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);
            updatedOrder.CancellationReason.Should().Be("Tôi muốn đổi địa chỉ giao hàng");

            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityReserved.Should().Be(0);
            updatedInv.QuantityAvailable.Should().Be(10);
        }
        #endregion

        #region TC07: SHIELD CHẶN KHÁCH TỰ HỦY ĐƠN KHI ĐÃ XUẤT KHO / ĐANG GIAO
        /// <summary>
        /// TC07: Kiểm tra khi đơn hàng đã chuyển sang Shipping, khách gọi API tự hủy sẽ bị ném InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task CancelOrderAsync_WhenShippingOrCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-SHIPPING-NOW",
                CustomerId = 10,
                Status = OrderStatus.Shipping,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new ShopOrderService(context, _mapper, CreateRoutingService(context));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelOrderAsync(10, "ORD-SHIPPING-NOW", "Hủy"));
        }
        #endregion
    }
}
