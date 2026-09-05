using backend.Data;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Xunit;

namespace backend.Tests.Modules.Module16_Dashboard
{
    /// <summary>
    /// ============================================================================
    /// MODULE 16: EXECUTIVE DASHBOARDS & ANALYTICS
    /// TEST SUITE: DashboardServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện 4 bảng điều khiển quản trị:
    /// - Dashboard 1: Tổng quan Kinh doanh & Doanh thu (Overview)
    /// - Dashboard 2: Doanh số & Phân bổ Địa lý (Sales & Geography)
    /// - Dashboard 3: Tồn kho & Sức chứa Kho hàng (Inventory & Capacity)
    /// - Dashboard 4: Chất lượng & Hạn dùng Nông sản FEFO (Quality & Expiry)
    /// </summary>
    public class DashboardServiceTests
    {
        // ========================================================================
        // 1. DASHBOARD 1: OVERVIEW METRICS
        // ========================================================================
        [Fact]
        public async Task GetOverviewAsync_ShouldCalculateCorrectMetricsAndGrowth()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var customer = new Customer
            {
                Id = 1,
                Code = "CUST-001",
                Name = "Nguyễn Văn A",
                PhoneNumber = "0901234567"
            };
            context.Customers.Add(customer);

            var now = DateTime.UtcNow;
            // Completed order in current period
            context.Orders.Add(new Order
            {
                Id = 1,
                OrderCode = "ORD-001",
                CustomerId = 1,
                Status = OrderStatus.Completed,
                TotalAmount = 500_000,
                PaymentMethod = PaymentMethod.EWallet,
                OrderDate = now.AddDays(-2)
            });
            // Pending order in current period
            context.Orders.Add(new Order
            {
                Id = 2,
                OrderCode = "ORD-002",
                CustomerId = 1,
                Status = OrderStatus.Pending,
                TotalAmount = 300_000,
                PaymentMethod = PaymentMethod.COD,
                OrderDate = now.AddDays(-1)
            });
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetOverviewAsync("7days", null, null);

            // Assert
            result.Should().NotBeNull();
            result.TotalOrders.Should().Be(2);
            result.CompletedOrders.Should().Be(1);
            result.TotalRevenue.Should().Be(500_000);
            result.AverageOrderValue.Should().Be(500_000);
            result.FulfillmentRatePercent.Should().Be(50);
            result.OrderStatusPipeline.Should().HaveCount(2);
            result.PaymentMethodBreakdown.Should().Contain(p => p.Method == "EWallet" && p.Amount == 500_000);
            result.RecentOrders.Should().HaveCount(2);
        }

        // ========================================================================
        // 2. DASHBOARD 2: SALES & GEOGRAPHY METRICS
        // ========================================================================
        [Fact]
        public async Task GetSalesGeographyAsync_ShouldAggregateTopProductsAndLocations()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var group = new ProductCategoryGroup { Id = 1, Name = "Trái cây tươi", Code = "TC" };
            var cat = new ProductCategory { Id = 1, Name = "Trái cây đặc sản", Code = "TCDS", CategoryGroupId = 1 };
            var prod = new Product { Id = 1, Name = "Xoài Cát Hòa Lộc", Code = "XOAI-CAT", CategoryId = 1 };
            var uom = new UoM { Id = 1, Name = "Kg", Code = "KG" };
            var variant = new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Name = "Xoài Cát Loại 1",
                Code = "SKU-XOAI-01",
                InventoryGuideline = 10
            };
            var tier = new CustomerTier { Id = 1, Name = "VIP Vàng", Code = "VIP-GOLD", MinSpending = 10_000_000, DiscountPercent = 10 };
            var customer = new Customer
            {
                Id = 1,
                Code = "CUST-001",
                Name = "Trần Thị B",
                PhoneNumber = "0912345678",
                CustomerTierId = 1
            };

            context.ProductCategoryGroups.Add(group);
            context.ProductCategories.Add(cat);
            context.Products.Add(prod);
            context.UoMs.Add(uom);
            context.ProductVariants.Add(variant);
            context.CustomerTiers.Add(tier);
            context.Customers.Add(customer);

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-2026-001",
                CustomerId = 1,
                Status = OrderStatus.Completed,
                TotalAmount = 400_000,
                DeliveryAddress = "Số 123 Đường Láng, Đống Đa, Hà Nội",
                OrderDate = DateTime.UtcNow.AddDays(-5)
            };
            order.Details.Add(new OrderDetail
            {
                Id = 1,
                OrderId = 1,
                VariantId = 1,
                UoMId = 1,
                Quantity = 4,
                UnitPrice = 100_000,
                TotalPrice = 400_000
            });
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetSalesGeographyAsync("30days", null, null);

            // Assert
            result.Should().NotBeNull();
            result.TopProducts.Should().HaveCount(1);
            result.TopProducts[0].Name.Should().Be("Xoài Cát Loại 1");
            result.TopProducts[0].QuantitySold.Should().Be(4);
            result.TopProducts[0].TotalRevenue.Should().Be(400_000);

            result.CategoryBreakdown.Should().HaveCount(1);
            result.CategoryBreakdown[0].CategoryGroupName.Should().Be("Trái cây tươi");

            result.GeographyBreakdown.Should().HaveCount(1);
            result.GeographyBreakdown[0].ProvinceName.Should().Be("Hà Nội");

            result.CustomerTierBreakdown.Should().HaveCount(1);
            result.CustomerTierBreakdown[0].TierName.Should().Be("VIP Vàng");
        }

        // ========================================================================
        // 3. DASHBOARD 3: INVENTORY & CAPACITY METRICS
        // ========================================================================
        [Fact]
        public async Task GetInventoryCapacityAsync_ShouldCalculatePhysicalCapacityAndAlerts()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var warehouse = new Warehouse
            {
                Id = 1,
                Name = "Tổng Kho Hà Nội",
                Code = "WH-HN",
                TotalCapacityCbm = 100,
                MaxWeightCapacityKg = 10_000,
                TotalAreaSqm = 500,
                WarningThresholdPercent = 85,
                IsActive = true
            };
            var variant = new ProductVariant
            {
                Id = 1,
                Name = "Gạo ST25 5kg",
                Code = "GAO-ST25-5K",
                UnitCbm = 0.01m,
                GrossWeightKg = 5.2m,
                InventoryGuideline = 50
            };
            var inv = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                QuantityAvailable = 20, // Lower than Guideline 50 -> low stock alert
                QuantityReserved = 5,
                QuantityQC = 2,
                QuantityDamaged = 1
            };
            var price = new ProductVariantPrice
            {
                Id = 1,
                VariantId = 1,
                UoMId = 1,
                Price = 180_000,
                IsActive = true
            };

            context.Warehouses.Add(warehouse);
            context.ProductVariants.Add(variant);
            context.WarehouseInventories.Add(inv);
            context.ProductVariantPrices.Add(price);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetInventoryCapacityAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalActiveWarehouses.Should().Be(1);
            result.WarehouseCapacities.Should().HaveCount(1);
            var whCap = result.WarehouseCapacities[0];
            whCap.WarehouseName.Should().Be("Tổng Kho Hà Nội");
            // Total qty = 28 units * 0.01 CBM = 0.28 CBM
            whCap.OccupiedCbm.Should().Be(0.28m);
            whCap.Status.Should().Be("Safe");

            // Compartments
            result.InventoryCompartments.AvailableQty.Should().Be(20);
            result.InventoryCompartments.ReservedQty.Should().Be(5);
            result.InventoryCompartments.InQcQty.Should().Be(2);
            result.InventoryCompartments.DamagedQty.Should().Be(1);
            result.InventoryCompartments.TotalValue.Should().Be(20 * 180_000);

            // Low Stock Alert
            result.LowStockAlerts.Should().HaveCount(1);
            result.LowStockAlerts[0].VariantCode.Should().Be("GAO-ST25-5K");
            result.LowStockAlerts[0].AvailableQty.Should().Be(20);
        }

        // ========================================================================
        // 4. DASHBOARD 4: QUALITY & EXPIRY METRICS
        // ========================================================================
        [Fact]
        public async Task GetQualityExpiryAsync_ShouldCategorizeBatchesAndCalculateRejectRate()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var today = DateTime.UtcNow.Date;
            var warehouse = new Warehouse
            {
                Id = 1,
                Name = "Kho Lạnh Đà Lạt",
                Code = "WH-DL",
                TotalCapacityCbm = 100,
                MaxWeightCapacityKg = 10_000,
                TotalAreaSqm = 500,
                IsActive = true
            };
            var supplier = new Supplier
            {
                Id = 1,
                Code = "SUP-001",
                Name = "Nông Trại Đà Lạt",
                Phone = "0912345678",
                Email = "dalat@farm.vn"
            };
            var product = new Product
            {
                Id = 1,
                Name = "Dâu Tây",
                Code = "DAU-TAY",
                CategoryId = 1
            };
            var variant = new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Product = product,
                Name = "Dâu Tây Đà Lạt 500g",
                Code = "DAU-TAY-500G"
            };
            var batchExpired = new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-EXP",
                VariantId = 1,
                Variant = variant,
                SupplierId = 1,
                Supplier = supplier,
                ManufactureDate = today.AddDays(-20),
                ExpiryDate = today.AddDays(-2), // Expired
                IsActive = true
            };
            var batchCritical = new ProductBatch
            {
                Id = 2,
                BatchCode = "BATCH-CRIT",
                VariantId = 1,
                Variant = variant,
                SupplierId = 1,
                Supplier = supplier,
                ManufactureDate = today.AddDays(-10),
                ExpiryDate = today.AddDays(1), // Critical (<3 days)
                IsActive = true
            };
            var batchSafe = new ProductBatch
            {
                Id = 3,
                BatchCode = "BATCH-SAFE",
                VariantId = 1,
                Variant = variant,
                SupplierId = 1,
                Supplier = supplier,
                ManufactureDate = today.AddDays(-5),
                ExpiryDate = today.AddDays(15), // Safe (>7 days)
                IsActive = true
            };

            context.Warehouses.Add(warehouse);
            context.Suppliers.Add(supplier);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);
            context.ProductBatches.AddRange(batchExpired, batchCritical, batchSafe);

            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                Warehouse = warehouse,
                VariantId = 1,
                Variant = variant,
                BatchId = 1,
                Batch = batchExpired,
                QuantityAvailable = 10
            });
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 2,
                WarehouseId = 1,
                Warehouse = warehouse,
                VariantId = 1,
                Variant = variant,
                BatchId = 2,
                Batch = batchCritical,
                QuantityAvailable = 15
            });
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 3,
                WarehouseId = 1,
                Warehouse = warehouse,
                VariantId = 1,
                Variant = variant,
                BatchId = 3,
                Batch = batchSafe,
                QuantityAvailable = 50
            });

            // QC Inbound Receipts Details
            context.InventoryReceiptDetails.Add(new InventoryReceiptDetail
            {
                Id = 1,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                ExpectedQuantity = 100,
                AcceptedQuantity = 90,
                RejectedQuantity = 10,
                RejectReason = "Dập nát vỏ"
            });

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetQualityExpiryAsync();

            // Assert
            result.Should().NotBeNull();
            result.ExpiryOverview.ExpiredCount.Should().Be(1);
            result.ExpiryOverview.CriticalCount.Should().Be(1);
            result.ExpiryOverview.SafeCount.Should().Be(1);

            // Inbound QC
            result.TotalInboundItems.Should().Be(100);
            result.TotalRejectedItems.Should().Be(10);
            result.InboundQcRejectRatePercent.Should().Be(10);
            result.QcRejectReasons.Should().Contain(r => r.Reason == "Dập nát vỏ");

            // Expiring batches list should contain critical and expired
            result.ExpiringBatches.Should().HaveCount(2);
        }
    }
}
