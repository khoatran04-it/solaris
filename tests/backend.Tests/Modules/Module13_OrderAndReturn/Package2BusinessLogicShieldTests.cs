using AutoMapper;
using backend.Data;
using backend.DTOs.CustomerReturnDTOs;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module13_OrderAndReturn
{
    /// <summary>
    /// ============================================================================
    /// TEST SUITE: Package2BusinessLogicShieldTests
    /// ============================================================================
    /// Kiểm thử các khiên bảo vệ nghiệp vụ Gói 2:
    /// 1. Chặn can thiệp phí vận chuyển âm (ShippingFee < 0) trong luồng Storefront Checkout.
    /// 2. Chuẩn hóa cơ chế tính chiết khấu Hội viên Tier: Tính trên giá sau khuyến mãi sản phẩm (Post-Promo).
    /// 3. Hoàn tất phiếu trả hàng một phần (Partial Return) cập nhật trạng thái đơn thành PartiallyRefunded.
    /// 4. Hoàn tất phiếu trả hàng toàn bộ (Full Return) cập nhật trạng thái đơn thành Refunded.
    /// 5. Admin hủy đơn hàng đã thanh toán (Paid) tự động chuyển trạng thái thanh toán sang Refunded.
    /// 6. Khách hàng tự hủy đơn hàng đã thanh toán (Paid) tự động chuyển trạng thái thanh toán sang Refunded.
    /// </summary>
    public class Package2BusinessLogicShieldTests
    {
        private readonly IMapper _mapper;

        public Package2BusinessLogicShieldTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        private static async Task SeedDependenciesAsync(SolarisDbContext context)
        {
            // 1. Địa chỉ kho & Kho Bán Lẻ
            var whAddress = new WarehouseAddress
            {
                Id = 1,
                Province = "Hồ Chí Minh",
                District = "Quận 1",
                Ward = "Phường Bến Nghé",
                StreetAddress = "100 Lê Lợi",
                Latitude = 10.7760,
                Longitude = 106.7010
            };
            context.WarehouseAddresses.Add(whAddress);

            context.Warehouses.Add(new Warehouse
            {
                Id = 1,
                Code = "WH-RETAIL-01",
                Name = "Kho Bán Lẻ Q1",
                WarehouseType = WarehouseTypeConstants.Retail,
                AddressId = 1,
                Address = whAddress,
                IsActive = true
            });

            // 2. Nhà cung cấp
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "SUP-DALAT",
                Name = "Nông Trại Đà Lạt GAP",
                Phone = "02633888999",
                Email = "dalatgap@solaris.vn",
                TaxCode = "5801234567",
                IsActive = true
            });

            // 3. Hạng hội viên VIP (Giảm 10%)
            context.CustomerTiers.Add(new CustomerTier
            {
                Id = 1,
                Code = "TIER-VIP",
                Name = "Thành Viên VIP",
                DiscountPercent = 10m,
                IsActive = true
            });

            // 4. Khách hàng kèm địa chỉ giao hàng
            var customer = new Customer
            {
                Id = 1,
                Code = "CUST-001",
                Name = "Nguyễn Văn Khách",
                PhoneNumber = "0901234567",
                CustomerTierId = 1,
                IsActive = true
            };
            context.Customers.Add(customer);

            context.CustomerAddresses.Add(new CustomerAddress
            {
                Id = 1,
                CustomerId = 1,
                ReceiverName = "Nguyễn Văn Khách",
                Phone = "0901234567",
                Province = "Hồ Chí Minh",
                District = "Quận 1",
                Ward = "Phường Bến Nghé",
                StreetAddress = "123 Lê Duẩn",
                Latitude = 10.7769,
                Longitude = 106.7009,
                IsDefault = true
            });

            // 5. Đơn vị tính & Sản phẩm
            context.UoMs.Add(new UoM { Id = 1, Code = "HOP", Name = "Hộp", IsActive = true });
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "CAT-TRAICAY", Name = "Trái Cây", IsActive = true });
            
            context.Products.Add(new Product
            {
                Id = 1,
                Code = "PROD-DAUTAY",
                Name = "Dâu Tây Đà Lạt",
                CategoryId = 1,
                BaseUoMId = 1,
                IsActive = true
            });

            context.ProductVariants.Add(new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Code = "SKU-DAUTAY-500G",
                Name = "Dâu Tây Hộp 500g",
                UnitCbm = 0.001m,
                GrossWeightKg = 0.5m,
                IsActive = true
            });

            context.ProductVariantPrices.Add(new ProductVariantPrice
            {
                Id = 1,
                VariantId = 1,
                UoMId = 1,
                Price = 100000m,
                IsDefault = true,
                IsActive = true
            });

            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-01",
                VariantId = 1,
                SupplierId = 1,
                ExpiryDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            });

            // Tồn kho sẵn sàng 100 hộp
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // 6. Tài khoản quản trị hệ thống
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                CitizenId = "079090001122",
                Username = "admin",
                FullName = "Quản Trị Viên",
                PhoneNumber = "0909090909",
                Email = "admin@solaris.vn",
                PasswordHash = "hash",
                IsActive = true
            });

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: CHẶN PHÍ VẬN CHUYỂN ÂM (SHIPPING FEE MANIPULATION)

        [Fact]
        public async Task CheckoutAsync_WithNegativeShippingFee_ThrowsArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Giỏ hàng có 1 hộp dâu
            var cart = new ShoppingCart
            {
                Id = 1,
                CustomerId = 1,
                Items = new List<ShoppingCartItem>
                {
                    new() { Id = 1, CartId = 1, VariantId = 1, UoMId = 1, Quantity = 1 }
                }
            };
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var routingService = new OrderRoutingService(context, new DistanceService());
            var shopOrderService = new ShopOrderService(context, _mapper, routingService);

            var checkoutDto = new ShopCheckoutRequestDto
            {
                CustomerAddressId = 1,
                PaymentMethod = PaymentMethod.COD,
                ShippingFee = -50000m, // Cố tình truyền số âm để bớt tiền hàng
                Latitude = 10.7769,
                Longitude = 106.7009
            };

            // Act & Assert: Phải chặn đứng và ném ArgumentException
            var act = () => shopOrderService.CheckoutAsync(1, checkoutDto);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Phí vận chuyển không hợp lệ*");
        }

        #endregion

        #region TC02: CHUẨN HÓA CỘNG DỒN CHIẾT KHẤU HỘI VIÊN (TIER DISCOUNT STACKING)

        [Fact]
        public async Task CheckoutAsync_TierDiscountStackedOnPostPromotionPrice_CalculatesAccurately()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Tạo chiến dịch khuyến mãi Flash Sale 20% cho SKU Dâu Tây (100.000đ -> Giảm 20.000đ còn 80.000đ)
            var promo = new PromotionCampaign
            {
                Id = 1,
                Name = "Flash Sale Dâu Tây",
                IsPercentage = true,
                DiscountValue = 20m, // Giảm 20%
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(2),
                IsActive = true,
                PromotionVariants = new List<PromotionVariant>
                {
                    new() { Id = 1, PromotionCampaignId = 1, VariantId = 1 }
                }
            };
            context.PromotionCampaigns.Add(promo);

            // Khách hàng có hạng VIP (DiscountPercent = 10%)
            // Giỏ hàng: Mua 1 hộp dâu tây
            var cart = new ShoppingCart
            {
                Id = 1,
                CustomerId = 1,
                Items = new List<ShoppingCartItem>
                {
                    new() { Id = 1, CartId = 1, VariantId = 1, UoMId = 1, Quantity = 1 }
                }
            };
            context.ShoppingCarts.Add(cart);
            await context.SaveChangesAsync();

            var routingService = new OrderRoutingService(context, new DistanceService());
            var shopOrderService = new ShopOrderService(context, _mapper, routingService);

            var checkoutDto = new ShopCheckoutRequestDto
            {
                CustomerAddressId = 1,
                PaymentMethod = PaymentMethod.COD,
                ShippingFee = 15000m,
                Latitude = 10.7769,
                Longitude = 106.7009
            };

            // Act
            var orderResult = await shopOrderService.CheckoutAsync(1, checkoutDto);

            // Assert:
            // Giá gốc: 100.000đ
            // Khuyến mãi Flash Sale 20%: 20.000đ
            // Chiết khấu VIP 10% tính trên SubTotal (100.000đ * 10%): 10.000đ
            // Tổng giảm: 20.000đ + 10.000đ = 30.000đ (Đảm bảo TotalDiscount <= SubTotal)
            // Tổng thanh toán: 100.000đ - 30.000đ + 15.000đ ship = 85.000đ
            orderResult.SubTotal.Should().Be(100000m);
            orderResult.DiscountAmount.Should().Be(30000m);
            orderResult.TotalAmount.Should().Be(85000m);
        }

        #endregion

        #region TC03: HOÀN TẤT TRẢ HÀNG MỘT PHẦN (PARTIALLY REFUNDED)

        [Fact]
        public async Task CompleteReturnAsync_WhenPartialItemsReturned_SetsOrderStatusToPartiallyRefunded()
        {
            // Arrange: Đơn hàng gồm 2 dòng sản phẩm (Dâu: 5 hộp, Bơ: 5 hộp)
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Thêm SKU thứ hai: Bơ
            context.ProductVariants.Add(new ProductVariant
            {
                Id = 2,
                ProductId = 1,
                Code = "SKU-BO-034",
                Name = "Bơ 034 Hộp 1kg",
                UnitCbm = 0.002m,
                GrossWeightKg = 1.0m,
                IsActive = true
            });

            var order = new Order
            {
                Id = 100,
                OrderCode = "ORD-PARTIAL-TEST",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 1000000m,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new() { Id = 1001, VariantId = 1, UoMId = 1, Quantity = 5, UnitPrice = 100000m },
                    new() { Id = 1002, VariantId = 2, UoMId = 1, Quantity = 5, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);

            // Phiếu trả hàng chỉ trả 2 hộp Dâu (VariantId = 1)
            var customerReturn = new CustomerReturn
            {
                Id = 200,
                ReturnCode = "RET-PARTIAL-001",
                OrderId = 100,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Inspecting,
                ReturnDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<CustomerReturnDetail>
                {
                    new()
                    {
                        Id = 2001,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ReturnedQuantity = 2,
                        AcceptedQuantity = 2,
                        DamagedQuantity = 0,
                        UnitPrice = 100000m
                    }
                }
            };
            context.CustomerReturns.Add(customerReturn);
            await context.SaveChangesAsync();

            var returnService = new CustomerReturnService(context, _mapper);

            // Act: Hoàn tất phiếu trả hàng
            var result = await returnService.CompleteReturnAsync(200);

            // Assert:
            result.Should().BeTrue();
            var updatedOrder = await context.Orders.FindAsync(100);
            updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.PartiallyRefunded,
                "Chỉ mới trả 2 trong tổng số 10 sản phẩm nên trạng thái phải là PartiallyRefunded");
        }

        #endregion

        #region TC04: HOÀN TẤT TRẢ HÀNG TOÀN BỘ (REFUNDED)

        [Fact]
        public async Task CompleteReturnAsync_WhenAllItemsReturned_SetsOrderStatusToRefunded()
        {
            // Arrange: Đơn hàng chỉ có 1 mặt hàng 2 hộp dâu
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var order = new Order
            {
                Id = 101,
                OrderCode = "ORD-FULL-RETURN",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 200000m,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new() { Id = 1011, VariantId = 1, UoMId = 1, Quantity = 2, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);

            // Phiếu trả hàng trả toàn bộ 2 hộp dâu
            var customerReturn = new CustomerReturn
            {
                Id = 201,
                ReturnCode = "RET-FULL-001",
                OrderId = 101,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Inspecting,
                ReturnDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<CustomerReturnDetail>
                {
                    new()
                    {
                        Id = 2011,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ReturnedQuantity = 2,
                        AcceptedQuantity = 2,
                        DamagedQuantity = 0,
                        UnitPrice = 100000m
                    }
                }
            };
            context.CustomerReturns.Add(customerReturn);
            await context.SaveChangesAsync();

            var returnService = new CustomerReturnService(context, _mapper);

            // Act
            var result = await returnService.CompleteReturnAsync(201);

            // Assert:
            result.Should().BeTrue();
            var updatedOrder = await context.Orders.FindAsync(101);
            updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.Refunded,
                "Toàn bộ sản phẩm đã được hoàn trả về kho thì đơn hàng phải mang trạng thái Refunded");
        }

        #endregion

        #region TC05: ADMIN HỦY ĐƠN HÀNG ĐÃ THANH TOÁN (ORDER CANCEL REFUNDED)

        [Fact]
        public async Task OrderService_CancelAsync_WhenOrderPaid_SetsPaymentStatusToRefunded()
        {
            // Arrange: Đơn hàng Confirmed và đã thanh toán Paid (qua VNPAY/Chuyển khoản)
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var order = new Order
            {
                Id = 102,
                OrderCode = "ORD-PAID-CANCEL",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 500000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new() { Id = 1021, VariantId = 1, UoMId = 1, Quantity = 5, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var routingService = new OrderRoutingService(context, new DistanceService());
            var orderService = new OrderService(context, _mapper, routingService);

            // Act: Admin thực hiện hủy đơn
            var result = await orderService.CancelAsync(102, "Khách yêu cầu hủy đơn đã chuyển khoản trước");

            // Assert:
            result.Should().BeTrue();
            var cancelledOrder = await context.Orders.FindAsync(102);
            cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
            cancelledOrder.PaymentStatus.Should().Be(PaymentStatus.Refunded,
                "Đơn đã thanh toán khi bị hủy phải được ghi nhận trạng thái hoàn tiền Refunded");
        }

        #endregion

        #region TC06: KHÁCH HÀNG TỰ HỦY ĐƠN HÀNG ĐÃ THANH TOÁN QUA SHOP (SHOP CANCEL REFUNDED)

        [Fact]
        public async Task ShopOrderService_CancelOrderAsync_WhenPaid_SetsPaymentStatusToRefunded()
        {
            // Arrange: Khách hàng tự hủy đơn đang Pending mà đã thanh toán trước
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var order = new Order
            {
                Id = 103,
                OrderCode = "ORD-SHOP-CANCEL",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 300000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new() { Id = 1031, VariantId = 1, UoMId = 1, Quantity = 3, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var routingService = new OrderRoutingService(context, new DistanceService());
            var shopOrderService = new ShopOrderService(context, _mapper, routingService);

            // Act: Khách hàng hủy qua Shop
            var result = await shopOrderService.CancelOrderAsync(1, "ORD-SHOP-CANCEL", "Đổi ý muốn mua món khác");

            // Assert:
            result.Should().BeTrue();
            var cancelledOrder = await context.Orders.FindAsync(103);
            cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);
            cancelledOrder.PaymentStatus.Should().Be(PaymentStatus.Refunded,
                "Đơn khách tự hủy khi đã thanh toán phải chuyển thành Refunded");
        }

        #endregion

        #region TC07: ĐỒNG BỘ DỮ LIỆU HOÀN TIỀN LÊN BẢNG BÁO CÁO THỐNG KÊ (FINANCIAL DASHBOARD SYNC)

        [Fact]
        public async Task DashboardService_GetFinancialPerformanceAsync_SynchronizesRefundData_NetRevenueAndCashFlowBridge()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Đơn 1: Đã thanh toán online 500k, sau đó có đơn trả hàng RMA hoàn 200k (PartiallyRefunded)
            var order1 = new Order
            {
                Id = 201,
                OrderCode = "ORD-ONLINE-REFUND",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.PartiallyRefunded,
                PaymentMethod = PaymentMethod.BankTransfer,
                TotalAmount = 500000m,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new() { Id = 2011, VariantId = 1, UoMId = 1, Quantity = 5, UnitPrice = 100000m }
                }
            };

            // Đơn 2: Đơn COD 300k hoàn tất bình thường
            var order2 = new Order
            {
                Id = 202,
                OrderCode = "ORD-COD-NORMAL",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMethod = PaymentMethod.COD,
                TotalAmount = 300000m,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new() { Id = 2021, VariantId = 1, UoMId = 1, Quantity = 3, UnitPrice = 100000m }
                }
            };

            context.Orders.AddRange(order1, order2);

            // Phiếu hoàn trả RMA 200k cho Đơn 1
            var ret = new CustomerReturn
            {
                Id = 301,
                ReturnCode = "RMA-DASH-01",
                OrderId = 201,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Completed,
                ReturnDate = DateTime.UtcNow,
                RefundAmount = 200000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<CustomerReturnDetail>
                {
                    new()
                    {
                        Id = 3011,
                        VariantId = 1,
                        BatchId = 1,
                        ReturnedQuantity = 2,
                        AcceptedQuantity = 2,
                        UnitPrice = 100000m,
                        RefundAmount = 200000m
                    }
                }
            };

            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var dashboardService = new DashboardService(context);

            // Act
            var dashboard = await dashboardService.GetFinancialPerformanceAsync("30days", null, null);

            // Assert:
            dashboard.Should().NotBeNull();

            // 1. Đồng bộ Doanh thu gộp & Doanh thu thuần
            dashboard.GrossRevenue.Should().Be(800000m, "Doanh số gộp của 2 đơn hoàn tất là 500k + 300k = 800,000 đ");
            dashboard.CustomerRefunds.Should().Be(200000m, "Tiền hoàn khách RMA hoàn tất là 200,000 đ");
            dashboard.NetRevenue.Should().Be(600000m, "Doanh thu thuần = 800,000 - 200,000 = 600,000 đ");

            // 2. Đồng bộ Cầu nối dòng tiền (Cash Flow Bridge)
            dashboard.CashFlowBridge.OnlinePaymentInflow.Should().Be(500000m,
                "Tiền thanh toán online vẫn thu đủ 500,000 đ ban đầu (kể cả khi trạng thái đơn là PartiallyRefunded)");
            dashboard.CashFlowBridge.CodCollectedInflow.Should().Be(300000m, "Tiền COD thu được là 300,000 đ");
            dashboard.CashFlowBridge.CustomerRefundOutflow.Should().Be(200000m, "Dòng tiền ra do hoàn trả RMA là 200,000 đ");

            // 3. Đồng bộ Báo cáo Hao hụt & Rủi ro (Shrinkage Loss)
            dashboard.ShrinkageLoss.ReturnRefundLoss.Should().Be(200000m,
                "Tổn thất hoàn trả hàng lỗi trên báo cáo hao hụt phải khớp đúng 200,000 đ");
        }

        #endregion
    }
}
