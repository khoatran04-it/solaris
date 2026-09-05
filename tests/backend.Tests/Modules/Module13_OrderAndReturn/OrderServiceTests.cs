using AutoMapper;
using backend.Data;
using backend.DTOs.OrderDTOs;
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
    /// UNIT TEST: OrderService (Quản Lý Đơn Bán Hàng, Định Tuyến & Giữ Chỗ Tồn Kho)
    /// ============================================================================
    /// </summary>
    public class OrderServiceTests
    {
        private readonly IMapper _mapper;

        public OrderServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        private static IOrderRoutingService CreateRoutingService(SolarisDbContext context)
        {
            return new OrderRoutingService(context, new DistanceService());
        }

        #region TC01: TRUY VẤN DANH SÁCH ĐƠN HÀNG PHÂN TRANG VÀ LÀM PHẲNG DỮ LIỆU
        /// <summary>
        /// TC01: Kiểm tra hàm GetPagedAsync lọc theo từ khóa, phân trang và làm phẳng dữ liệu
        /// (CustomerName, CustomerPhone, WarehouseName, DiscountAmount) chính xác.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldReturnPagedOrdersWithRelationalFlattening()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Nguyễn Văn A", PhoneNumber = "0901234567", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-HCM", Name = "Kho Tổng TP.HCM", IsActive = true };
            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);

            var order1 = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260830-000001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                SubTotal = 200000m,
                ShippingFee = 30000m,
                TotalAmount = 230000m,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, OrderId = 1, VariantId = 10, UoMId = 1, Quantity = 2, UnitPrice = 100000m, DiscountAmount = 10000m, TotalPrice = 190000m }
                }
            };
            var order2 = new Order
            {
                Id = 2,
                OrderCode = "ORD-20260830-000002",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                SubTotal = 500000m,
                ShippingFee = 0m,
                TotalAmount = 500000m,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Orders.AddRange(order1, order2);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act
            var result = await service.GetPagedAsync(
                search: "000001",
                customerId: null,
                warehouseId: null,
                status: null,
                paymentStatus: null,
                startDate: null,
                endDate: null,
                pageIndex: 1,
                pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(1);
            var item = result.Items.First();
            item.OrderCode.Should().Be("ORD-20260830-000001");
            item.CustomerName.Should().Be("Nguyễn Văn A");
            item.CustomerPhone.Should().Be("0901234567");
            item.WarehouseName.Should().Be("Kho Tổng TP.HCM");
            item.DiscountAmount.Should().Be(10000m);
        }
        #endregion

        #region TC02: PHÂN QUYỀN TRUY CẬP KHO (DATA ISOLATION) TRONG DANH SÁCH ĐƠN HÀNG
        /// <summary>
        /// TC02: Kiểm tra tham số allowedWarehouseIds chỉ cho phép Thủ kho xem đơn hàng thuộc kho của mình.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_WithAllowedWarehouseIds_ShouldFilterWarehouseData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Khách Hàng", PhoneNumber = "0901112222", IsActive = true };
            var wh1 = new Warehouse { Id = 1, Code = "WH-1", Name = "Kho 1", IsActive = true };
            var wh2 = new Warehouse { Id = 2, Code = "WH-2", Name = "Kho 2", IsActive = true };

            var orderWh1 = new Order { Id = 1, OrderCode = "ORD-WH1", CustomerId = 1, WarehouseId = 1, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var orderWh2 = new Order { Id = 2, OrderCode = "ORD-WH2", CustomerId = 1, WarehouseId = 2, OrderDate = now, CreatedAt = now, UpdatedAt = now };

            context.Customers.Add(customer);
            context.Warehouses.AddRange(wh1, wh2);
            context.Orders.AddRange(orderWh1, orderWh2);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act: Thủ kho chỉ có quyền kho 2
            var result = await service.GetPagedAsync(
                search: null, customerId: null, warehouseId: null, status: null, paymentStatus: null,
                startDate: null, endDate: null, pageIndex: 1, pageSize: 10,
                allowedWarehouseIds: new List<int> { 2 });

            // Assert
            result.TotalRecords.Should().Be(1);
            result.Items.First().OrderCode.Should().Be("ORD-WH2");
        }
        #endregion

        #region TC03: TRUY VẤN CHI TIẾT ĐƠN HÀNG & DANH SÁCH LÔ HÀNG ĐÃ XUẤT THỰC TẾ
        /// <summary>
        /// TC03: Kiểm tra hàm GetByIdAsync trả về đầy đủ chi tiết đơn hàng và các dòng lô đã xuất kho thực tế (IssuedItems).
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnOrderWithIssuedItemsAndDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Lê Thị B", PhoneNumber = "0987654321", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "Kho Tổng", IsActive = true };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-CAM", Name = "Cam Sành", IsActive = true };
            var batch = new ProductBatch { Id = 5, BatchCode = "BATCH-CAM-01", VariantId = 10, ExpiryDate = now.AddDays(20) };

            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            context.UoMs.Add(uom);
            context.ProductVariants.Add(variant);
            context.ProductBatches.Add(batch);

            var order = new Order
            {
                Id = 100,
                OrderCode = "ORD-20260830-DETAIL",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Shipping,
                PaymentStatus = PaymentStatus.Paid,
                SubTotal = 150000m,
                TotalAmount = 150000m,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, VariantId = 10, UoMId = 1, Quantity = 5, UnitPrice = 30000m, TotalPrice = 150000m, IssuedQuantity = 5 }
                }
            };

            var issue = new InventoryIssue
            {
                Id = 1,
                IssueCode = "PXK-001",
                WarehouseId = 1,
                OrderId = 100,
                Status = InventoryIssueStatus.Completed,
                IssueDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<InventoryIssueDetail>
                {
                    new InventoryIssueDetail
                    {
                        Id = 1,
                        OrderDetailId = 1,
                        VariantId = 10,
                        BatchId = 5,
                        UoMId = 1,
                        Quantity = 5,
                        UnitPrice = 30000m,
                        TotalPrice = 150000m
                    }
                }
            };

            context.Orders.Add(order);
            context.InventoryIssues.Add(issue);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act
            var result = await service.GetByIdAsync(100);

            // Assert
            result.Should().NotBeNull();
            result.OrderCode.Should().Be("ORD-20260830-DETAIL");
            result.CustomerName.Should().Be("Lê Thị B");
            result.Details.Should().HaveCount(1);
            result.Details.First().VariantName.Should().Be("Cam Sành");
            result.IssuedItems.Should().HaveCount(1);
            result.IssuedItems.First().BatchCode.Should().Be("BATCH-CAM-01");
            result.IssuedItems.First().IssueCode.Should().Be("PXK-001");
        }
        #endregion

        #region TC04: KHỞI TẠO ĐƠN HÀNG MỚI & GIỮ CHỖ TỒN KHO (RESERVE)
        /// <summary>
        /// TC04: Kiểm tra hàm CreateAsync khởi tạo đơn hàng, tính toán tổng tiền và tự động
        /// giữ chỗ tồn kho (QuantityAvailable -= qty, QuantityReserved += qty) kèm ghi sổ cái InventoryTransaction.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreateOrderAndReserveInventory()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Trần C", PhoneNumber = "0911223344", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "Kho Chính", IsActive = true };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-TAO", Name = "Táo Envy", IsActive = true };
            var user = new IAUser
            {
                Id = 1,
                CitizenId = "001200000001",
                Username = "admin",
                FullName = "Administrator",
                Email = "admin@solaris.vn",
                PhoneNumber = "0901112222",
                PasswordHash = "hash",
                IsActive = true
            };

            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-TAO-01", VariantId = 10, ExpiryDate = now.AddDays(15) };
            var inventory = new WarehouseInventory { Id = 1, WarehouseId = 1, VariantId = 10, BatchId = 1, QuantityAvailable = 20, QuantityReserved = 0 };

            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            context.UoMs.Add(uom);
            context.ProductVariants.Add(variant);
            context.IAUsers.Add(user);
            context.ProductBatches.Add(batch);
            context.WarehouseInventories.Add(inventory);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            var createDto = new OrderCreateDto
            {
                CustomerId = 1,
                WarehouseId = 1,
                ReceiverName = "Trần C",
                ReceiverPhone = "0911223344",
                DeliveryAddress = "123 Lê Lợi, Q1, TP.HCM",
                PaymentMethod = PaymentMethod.COD,
                ShippingFee = 25000m,
                Details = new List<OrderDetailCreateDto>
                {
                    new OrderDetailCreateDto
                    {
                        VariantId = 10,
                        UoMId = 1,
                        Quantity = 5,
                        UnitPrice = 50000m,
                        DiscountAmount = 0
                    }
                }
            };

            // Act
            int orderId = await service.CreateAsync(createDto, currentUserId: 1);

            // Assert
            orderId.Should().BeGreaterThan(0);

            var createdOrder = await context.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == orderId);
            createdOrder.Should().NotBeNull();
            createdOrder!.Status.Should().Be(OrderStatus.Confirmed);
            createdOrder.SubTotal.Should().Be(250000m);
            createdOrder.TotalAmount.Should().Be(275000m);

            // Kiểm tra tồn kho đã bị khóa giữ chỗ
            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityAvailable.Should().Be(15);
            updatedInv.QuantityReserved.Should().Be(5);

            // Kiểm tra sổ cái bất biến đã ghi nhận giao dịch Reserve
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == createdOrder.OrderCode);
            txn.Should().NotBeNull();
            txn!.Type.Should().Be(TransactionType.Reserve);
            txn.Quantity.Should().Be(5);
        }
        #endregion

        #region TC05: KHỞI TẠO ĐƠN HÀNG VỚI GIÁ TRỐNG -> TỰ ĐỘNG TRA CỨU TỪ PRODUCTVARIANTPRICE
        /// <summary>
        /// TC05: Kiểm tra nếu DTO không truyền UnitPrice (hoặc = 0), hệ thống tự tra cứu giá từ ProductVariantPrice theo UoMId.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithZeroPrice_ShouldLookupFromProductVariantPrice()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Khách Hàng", PhoneNumber = "0911223344", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "Kho", IsActive = true };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-XOAI", Name = "Xoài Cát", IsActive = true };
            var price = new ProductVariantPrice { Id = 1, VariantId = 10, UoMId = 1, Price = 80000m, IsActive = true, IsDefault = true };

            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            context.UoMs.Add(uom);
            context.ProductVariants.Add(variant);
            context.ProductVariantPrices.Add(price);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            var createDto = new OrderCreateDto
            {
                CustomerId = 1,
                WarehouseId = 1,
                Details = new List<OrderDetailCreateDto>
                {
                    new OrderDetailCreateDto { VariantId = 10, UoMId = 1, Quantity = 2, UnitPrice = null }
                }
            };

            // Act
            int orderId = await service.CreateAsync(createDto);

            // Assert
            var order = await context.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == orderId);
            order.Should().NotBeNull();
            order!.Details.First().UnitPrice.Should().Be(80000m);
            order.SubTotal.Should().Be(160000m);
        }
        #endregion

        #region TC06: SHIELD KIỂM TRA ĐƠN HÀNG RỖNG
        /// <summary>
        /// TC06: Kiểm tra tạo đơn hàng không có dòng sản phẩm nào sẽ ném ArgumentException.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithEmptyDetails_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            var createDto = new OrderCreateDto
            {
                CustomerId = 1,
                Details = new List<OrderDetailCreateDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(createDto));
        }
        #endregion

        #region TC07: SHIELD KIỂM TRA KHÁCH HÀNG KHÔNG TỒN TẠI
        /// <summary>
        /// TC07: Kiểm tra tạo đơn hàng với CustomerId không tồn tại sẽ ném ArgumentException.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithInvalidCustomer_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            var createDto = new OrderCreateDto
            {
                CustomerId = 999,
                Details = new List<OrderDetailCreateDto>
                {
                    new OrderDetailCreateDto { VariantId = 1, UoMId = 1, Quantity = 1 }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(createDto));
        }
        #endregion

        #region TC08: CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG (PARTIAL UPDATE)
        /// <summary>
        /// TC08: Kiểm tra hàm UpdateStatusAsync cập nhật từng phần (Status, PaymentStatus, Note).
        /// </summary>
        [Fact]
        public async Task UpdateStatusAsync_ShouldUpdatePartialFields()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-UPDATE",
                CustomerId = 1,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Unpaid,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            var updateDto = new OrderUpdateDto
            {
                Status = OrderStatus.Processing,
                PaymentStatus = PaymentStatus.Paid,
                Note = "Giao hàng buổi sáng"
            };

            // Act
            bool result = await service.UpdateStatusAsync(1, updateDto);

            // Assert
            result.Should().BeTrue();
            var updated = await context.Orders.FindAsync(1);
            updated!.Status.Should().Be(OrderStatus.Processing);
            updated.PaymentStatus.Should().Be(PaymentStatus.Paid);
            updated.Note.Should().Be("Giao hàng buổi sáng");
        }
        #endregion

        #region TC09: HỦY ĐƠN HÀNG & HOÀN TRẢ TỒN KHO (UNRESERVE)
        /// <summary>
        /// TC09: Kiểm tra hàm CancelAsync chuyển trạng thái sang Cancelled và tự động
        /// hoàn trả số lượng đã giữ chỗ về tồn khả dụng (QuantityReserved -= qty, QuantityAvailable += qty).
        /// </summary>
        [Fact]
        public async Task CancelAsync_ShouldCancelOrderAndUnreserveInventory()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-CANCEL",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, VariantId = 10, UoMId = 1, Quantity = 4, UnitPrice = 50000m, TotalPrice = 200000m, IssuedQuantity = 0 }
                }
            };

            var inventory = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 10,
                BatchId = 1,
                QuantityAvailable = 6,
                QuantityReserved = 4
            };

            context.Orders.Add(order);
            context.WarehouseInventories.Add(inventory);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act
            bool result = await service.CancelAsync(1, "Khách đổi ý không mua nữa", currentUserId: 1);

            // Assert
            result.Should().BeTrue();

            var updatedOrder = await context.Orders.FindAsync(1);
            updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);
            updatedOrder.CancellationReason.Should().Be("Khách đổi ý không mua nữa");

            // Tồn kho đã được nhả
            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityReserved.Should().Be(0);
            updatedInv.QuantityAvailable.Should().Be(10);

            // Sổ cái ghi nhận Unreserve
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "ORD-CANCEL" && t.Type == TransactionType.Unreserve);
            txn.Should().NotBeNull();
            txn!.Quantity.Should().Be(4);
        }
        #endregion

        #region TC10: SHIELD CHẶN HỦY ĐƠN HÀNG ĐANG GIAO HOẶC ĐÃ HOÀN TẤT
        /// <summary>
        /// TC10: Kiểm tra đơn hàng đang ở trạng thái Shipping hoặc Completed sẽ bị chặn hủy (InvalidOperationException).
        /// </summary>
        [Fact]
        public async Task CancelAsync_WhenShippingOrCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-SHIPPED",
                CustomerId = 1,
                Status = OrderStatus.Shipping,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(1, "Hủy đơn đang giao"));
        }
        #endregion

        #region TC11: XÓA MỀM ĐƠN HÀNG CHƯA XUẤT KHO HOẶC ĐÃ HỦY
        /// <summary>
        /// TC11: Kiểm tra hàm DeleteAsync xóa mềm đơn hàng ở trạng thái Confirmed hoặc Cancelled.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenConfirmedOrCancelled_ShouldSoftDelete()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-DELETE",
                CustomerId = 1,
                Status = OrderStatus.Confirmed,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act
            bool result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deletedOrder = await context.Orders.FindAsync(1);
            deletedOrder!.IsDeleted.Should().BeTrue();
            deletedOrder.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC12: SHIELD CHẶN XÓA ĐƠN HÀNG ĐANG GIAO / HOÀN TẤT
        /// <summary>
        /// TC12: Kiểm tra đơn hàng đang giao hoặc đã hoàn tất không được phép xóa (InvalidOperationException).
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenProcessingOrCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-COMPLETED",
                CustomerId = 1,
                Status = OrderStatus.Completed,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context, _mapper, CreateRoutingService(context));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1));
        }
        #endregion
    }
}
