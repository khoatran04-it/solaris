using backend.Data;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace backend.Tests.Modules.Module16_Dashboard
{
    /// <summary>
    /// ============================================================================
    /// TEST SUITE: FinancialAndDashboardShieldTests (Đợt 3)
    /// ============================================================================
    /// Kiểm thử tính toàn vẹn chỉ số Tài chính, Giá vốn và Phân quyền Kho trên Dashboard:
    /// 1. TC01: Hoàn nhập giá vốn (COGS Reversal) theo chuẩn VAS 14 / TT 200 khi khách trả hàng
    /// 2. TC02: Phân quyền kho (RBAC) trên Dashboard Chất lượng & Tỷ lệ lỗi QC
    /// 3. TC03: Tính bình quân gia quyền giá nhập chuẩn xác theo khối lượng mua PO thực tế
    /// </summary>
    public class FinancialAndDashboardShieldTests
    {
        #region TC01: Hoàn nhập giá vốn (COGS Reversal) khi khách hoàn trả hàng
        [Fact]
        public async Task TC01_FinancialPerformance_CogsReversalOnCustomerReturn_AccuratelyReversesGrossProfit()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var now = DateTime.UtcNow;

            // 1. Cấu hình Sản phẩm & Ngành hàng
            var group = new ProductCategoryGroup { Id = 1, Name = "Trái cây xuất khẩu", Code = "TCXK" };
            var category = new ProductCategory { Id = 1, Name = "Dưa hấu ruột đỏ", Code = "DUA-HAU", CategoryGroupId = 1 };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var product = new Product { Id = 1, Code = "PROD-DUA", Name = "Dưa Hấu Long An", CategoryId = 1, BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 1, ProductId = 1, Code = "SKU-DUA-KG", Name = "Dưa Hấu Long An Loại 1", IsActive = true };

            context.ProductCategoryGroups.Add(group);
            context.ProductCategories.Add(category);
            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);

            // 2. Đơn mua hàng PO (Giá vốn nhập từ NCC: 70,000 VND / kg)
            var supplier = new Supplier { Id = 1, Code = "SUP-LONGAN", Name = "HTX Nông Nghiệp Long An", Phone = "0901234567", Email = "longan@solaris.vn", IsActive = true };
            context.Suppliers.Add(supplier);

            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-2026-001",
                SupplierId = 1,
                Status = PurchaseOrderStatus.Completed,
                OrderDate = now.AddDays(-10),
                TotalAmount = 35_000_000
            };
            var poDetail = new PurchaseOrderDetail
            {
                Id = 1,
                PurchaseOrderId = 1,
                VariantId = 1,
                UoMId = 1,
                OrderQuantity = 500,
                UnitPrice = 70_000
            };
            context.PurchaseOrders.Add(po);
            context.PurchaseOrderDetails.Add(poDetail);

            // 3. Khách hàng & Đơn bán hàng (Bán 100 kg giá 100,000 VND/kg = 10,000,000 VND)
            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Chị Lan Bách Hóa", PhoneNumber = "0901234567" };
            context.Customers.Add(customer);

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-2026-001",
                CustomerId = 1,
                Status = OrderStatus.Completed,
                PaymentMethod = PaymentMethod.BankTransfer,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 10_000_000,
                OrderDate = now.AddDays(-3)
            };
            var orderDetail = new OrderDetail
            {
                Id = 1,
                OrderId = 1,
                VariantId = 1,
                UoMId = 1,
                Quantity = 100,
                BaseQuantity = 100,
                UnitPrice = 100_000,
                TotalPrice = 10_000_000
            };
            context.Orders.Add(order);
            context.OrderDetails.Add(orderDetail);

            // 4. Khách hoàn trả 20 kg (Hoàn tiền 2,000,000 VND)
            // Chuẩn VAS 14 / TT 200:
            // - Giảm trừ Doanh thu: 2,000,000 VND (Doanh thu thuần = 10tr - 2tr = 8,000,000 VND)
            // - Hoàn nhập Giá vốn: 20 kg * 70,000 = 1,400,000 VND (Giá vốn thuần = 7tr - 1.4tr = 5,600,000 VND)
            // - Lợi nhuận gộp thực tế: 8,000,000 - 5,600,000 = 2,400,000 VND (Biên LN gộp = 30%)
            var returnOrder = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-2026-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Completed,
                RefundAmount = 2_000_000,
                ReturnDate = now.AddDays(-1)
            };
            var returnDetail = new CustomerReturnDetail
            {
                Id = 1,
                CustomerReturnId = 1,
                VariantId = 1,
                UoMId = 1,
                ReturnedQuantity = 20,
                AcceptedQuantity = 20,
                DamagedQuantity = 0,
                UnitPrice = 100_000,
                RefundAmount = 2_000_000
            };
            context.CustomerReturns.Add(returnOrder);
            context.CustomerReturnDetails.Add(returnDetail);

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetFinancialPerformanceAsync("month", now.AddDays(-15), now.AddDays(1));

            // Assert
            result.Should().NotBeNull();
            result.GrossRevenue.Should().Be(10_000_000);
            result.CustomerRefunds.Should().Be(2_000_000);
            result.NetRevenue.Should().Be(8_000_000);

            // Giá vốn thuần sau hoàn nhập phải là 5,600,000 VND (thay vì 7,000,000 VND)
            result.TotalCogs.Should().Be(5_600_000);

            // Lợi nhuận gộp chuẩn xác: 8,000,000 - 5,600,000 = 2,400,000 VND (thay vì 1,000,000 VND)
            result.GrossProfit.Should().Be(2_400_000);
            result.GrossMarginPercent.Should().Be(30.0m);
        }
        #endregion

        #region TC02: Phân quyền kho (RBAC) trên Dashboard Chất lượng & Tỷ lệ lỗi QC
        [Fact]
        public async Task TC02_QualityExpiry_WithWarehouseRbac_FiltersQcRejectRateAndReturnRate()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var now = DateTime.UtcNow;

            // Seed 2 kho
            context.Warehouses.AddRange(
                new Warehouse { Id = 1, Code = "WH-HN", Name = "Kho Hà Nội", IsActive = true },
                new Warehouse { Id = 2, Code = "WH-DN", Name = "Kho Đà Nẵng", IsActive = true }
            );

            // Kho 1: Nhập 100 cái, từ chối 20 cái lỗi (Tỷ lệ lỗi 20%)
            var rec1 = new InventoryReceipt
            {
                Id = 1,
                ReceiptCode = "IR-HN-01",
                WarehouseId = 1,
                Status = InventoryReceiptStatus.Completed,
                CreatedAt = now.AddDays(-2)
            };
            var recDetail1 = new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 1,
                VariantId = 1,
                UoMId = 1,
                AcceptedQuantity = 80,
                RejectedQuantity = 20,
                RejectReason = "Dập nát nhiều"
            };
            context.InventoryReceipts.Add(rec1);
            context.InventoryReceiptDetails.Add(recDetail1);

            // Kho 1: 5 đơn hàng, 1 đơn trả lại
            for (int i = 1; i <= 5; i++)
            {
                context.Orders.Add(new Order { Id = i, OrderCode = $"ORD-HN-{i}", WarehouseId = 1, Status = OrderStatus.Completed, OrderDate = now.AddDays(-1) });
            }
            context.CustomerReturns.Add(new CustomerReturn { Id = 1, ReturnCode = "RET-HN-01", WarehouseId = 1, Status = CustomerReturnStatus.Completed, ReturnDate = now.AddDays(-1) });

            // Kho 2: Nhập 100 cái, đạt chuẩn 100%, 0 lỗi (Tỷ lệ lỗi 0%)
            var rec2 = new InventoryReceipt
            {
                Id = 2,
                ReceiptCode = "IR-DN-01",
                WarehouseId = 2,
                Status = InventoryReceiptStatus.Completed,
                CreatedAt = now.AddDays(-2)
            };
            var recDetail2 = new InventoryReceiptDetail
            {
                Id = 2,
                InventoryReceiptId = 2,
                VariantId = 1,
                UoMId = 1,
                AcceptedQuantity = 100,
                RejectedQuantity = 0
            };
            context.InventoryReceipts.Add(rec2);
            context.InventoryReceiptDetails.Add(recDetail2);

            // Kho 2: 10 đơn hàng, 0 đơn trả lại
            for (int i = 6; i <= 15; i++)
            {
                context.Orders.Add(new Order { Id = i, OrderCode = $"ORD-DN-{i}", WarehouseId = 2, Status = OrderStatus.Completed, OrderDate = now.AddDays(-1) });
            }

            await context.SaveChangesAsync();

            // Act: Quản lý Kho Đà Nẵng (WH2) truy vấn Dashboard
            var result = await service.GetQualityExpiryAsync(allowedWarehouseIds: new List<int> { 2 });

            // Assert: Chỉ thấy số liệu độc quyền của Kho 2
            result.Should().NotBeNull();
            result.InboundQcRejectRatePercent.Should().Be(0);
            result.TotalInboundItems.Should().Be(100);
            result.TotalRejectedItems.Should().Be(0);

            result.CustomerReturnRatePercent.Should().Be(0);
            result.TotalOrders.Should().Be(10);
            result.TotalReturnOrders.Should().Be(0);
        }
        #endregion

        #region TC03: Tính bình quân gia quyền giá nhập chuẩn xác theo khối lượng mua PO thực tế
        [Fact]
        public async Task TC03_PriceVolatility_PureWeightedAverageOnPurchases_DoesNotDistortDenomByPriceAnnouncements()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new DashboardService(context);

            var now = DateTime.UtcNow;

            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var product = new Product { Id = 1, Code = "PROD-XOAI", Name = "Xoài Cát", BaseUoMId = 1, IsActive = true };
            var variant = new ProductVariant { Id = 1, ProductId = 1, Code = "SKU-XOAI-KG", Name = "Xoài Cát Kg", IsActive = true };

            context.UoMs.Add(uom);
            context.Products.Add(product);
            context.ProductVariants.Add(variant);

            var supplier = new Supplier { Id = 1, Code = "SUP-XOAI", Name = "NCC Xoài", Phone = "0901234567", Email = "xoai@solaris.vn", IsActive = true };
            context.Suppliers.Add(supplier);

            // PO thực tế: Nhập 1,000 kg với đơn giá 20,000 VND
            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-XOAI-01",
                SupplierId = 1,
                Status = PurchaseOrderStatus.Completed,
                OrderDate = now.AddDays(-10),
                TotalAmount = 20_000_000
            };
            var poDetail = new PurchaseOrderDetail
            {
                Id = 1,
                PurchaseOrderId = 1,
                PurchaseOrder = po,
                VariantId = 1,
                UoMId = 1,
                OrderQuantity = 1000,
                UnitPrice = 20_000
            };
            po.Details.Add(poDetail);
            context.PurchaseOrders.Add(po);
            context.PurchaseOrderDetails.Add(poDetail);

            // Trong kỳ có 1 thông báo điều chỉnh giá chào của NCC lên 30,000 VND (chưa phát sinh mua)
            context.SupplierProductPriceHistories.Add(new SupplierProductPriceHistory
            {
                Id = 1,
                VariantId = 1,
                PurchaseUoMId = 1,
                OldPrice = 20_000,
                NewPrice = 30_000,
                EffectiveDate = now.AddDays(-5)
            });

            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPriceVolatilityAsync(variantId: 1, timeframe: "month", fromDate: now.AddDays(-15), toDate: now.AddDays(1));

            // Assert
            result.Should().NotBeNull();
            result.Timeline.Should().NotBeEmpty();

            // Điểm mốc thời gian có phát sinh PO phải phản ánh đúng giá mua thực tế 20,000 VND
            // Không bị chia nhầm mẫu số (1000 + 1) làm méo mó thành 19,990 VND
            var point = result.Timeline.Last();
            point.AvgImportPrice.Should().Be(20_000);
        }
        #endregion
    }
}
