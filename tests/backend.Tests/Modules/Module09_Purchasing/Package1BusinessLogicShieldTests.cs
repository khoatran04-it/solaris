using AutoMapper;
using backend.Data;
using backend.DTOs.CustomerReturnDTOs;
using backend.DTOs.InventoryReceiptDTOs;
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

namespace backend.Tests.Modules.Module09_Purchasing
{
    /// <summary>
    /// ============================================================================
    /// TEST SUITE: Package1BusinessLogicShieldTests
    /// ============================================================================
    /// Kiểm thử các khiên bảo vệ nghiệp vụ (Business Logic Shields) Gói 1:
    /// 1. Chặn nhận hàng vào PO đã Hoàn tất (Completed) hoặc đã Hủy (Cancelled)
    /// 2. Chặn nhận hàng vượt quá số lượng đặt còn lại trên 10% (Over-Receiving Shield)
    /// 3. Cho phép nhận hàng khi nằm trong giới hạn dung sai cho phép (<= 10%)
    /// 4. Admin đổi trạng thái đơn hàng sang Hủy (Cancelled) giải phóng đúng lượng tồn kho giữ chỗ (Unreserve)
    /// 5. Chặn tạo phiếu trả hàng RMA cho đơn hàng chưa xuất kho/chưa hoàn tất (Draft, Pending, Cancelled)
    /// 6. Chặn trả hàng vượt số lượng đã mua trong đơn hàng gốc
    /// 7. Chống hoàn tiền lặp lại (Double-Refund) khi tạo nhiều phiếu trả hàng cho cùng một sản phẩm
    /// </summary>
    public class Package1BusinessLogicShieldTests
    {
        private readonly IMapper _mapper;

        public Package1BusinessLogicShieldTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        private static async Task SeedDependenciesAsync(SolarisDbContext context)
        {
            // 1. Kho Tổng & Kho Bán Lẻ
            context.Warehouses.AddRange(
                new Warehouse
                {
                    Id = 1,
                    Code = "WH-MASTER",
                    Name = "Tổng Kho Củ Chi",
                    WarehouseType = WarehouseTypeConstants.MasterHub,
                    IsActive = true
                },
                new Warehouse
                {
                    Id = 2,
                    Code = "WH-RETAIL",
                    Name = "Kho Bán Lẻ Q1",
                    WarehouseType = WarehouseTypeConstants.Retail,
                    IsActive = true
                }
            );

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

            // 3. Khách hàng
            context.Customers.Add(new Customer
            {
                Id = 1,
                Code = "CUST-001",
                Name = "Trần Thị Khách",
                PhoneNumber = "0912345678",
                IsActive = true
            });

            // 4. Đơn vị tính
            context.UoMs.Add(new UoM { Id = 1, Code = "HOP", Name = "Hộp", IsActive = true });

            // 5. Sản phẩm & Biến thể & Lô hàng
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
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-DAUTAY-01",
                VariantId = 1,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsActive = true
            });

            // 6. Tài khoản quản trị
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

        #region NHÓM 1: KHIÊN BẢO VỆ NHẬP HÀNG MUA VÀO (INBOUND PO SHIELDS)

        [Fact]
        public async Task CreateReceipt_WhenPOIsCompleted_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 10,
                OrderCode = "PO-COMPLETED-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.Completed, // Đã hoàn tất
                Details = new List<PurchaseOrderDetail>
                {
                    new() { Id = 100, VariantId = 1, UoMId = 1, OrderQuantity = 50, ReceivedQuantity = 50, UnitPrice = 80000m }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        PurchaseOrderDetailId = 100,
                        ExpectedQuantity = 10,
                        AcceptedQuantity = 10
                    }
                }
            };

            // Act & Assert: Chặn lập phiếu nhập cho PO đã Completed
            var act = () => service.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã Hoàn tất (Completed)*");
        }

        [Fact]
        public async Task CreateReceipt_WhenPOIsCancelled_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 11,
                OrderCode = "PO-CANCELLED-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.Cancelled, // Đã hủy
                CancellationReason = "NCC hủy hợp đồng",
                Details = new List<PurchaseOrderDetail>
                {
                    new() { Id = 101, VariantId = 1, UoMId = 1, OrderQuantity = 50, ReceivedQuantity = 0, UnitPrice = 80000m }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        PurchaseOrderDetailId = 101,
                        ExpectedQuantity = 20,
                        AcceptedQuantity = 20
                    }
                }
            };

            // Act & Assert: Chặn lập phiếu nhập cho PO đã Cancelled
            var act = () => service.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã bị Hủy (Cancelled)*");
        }

        [Fact]
        public async Task CreateReceipt_WhenQuantityExceedsPOTolerance_ThrowsInvalidOperationException()
        {
            // Arrange: PO đặt 100 hộp, đã nhận 60 hộp -> Còn lại 40 hộp.
            // Dung sai tối đa 10% = 40 * 1.10 = 44 hộp.
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 12,
                OrderCode = "PO-TOLERANCE-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.PartiallyReceived,
                Details = new List<PurchaseOrderDetail>
                {
                    new() { Id = 102, VariantId = 1, UoMId = 1, OrderQuantity = 100, ReceivedQuantity = 60, UnitPrice = 80000m }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Cố tình nhập 45 hộp (> 44 hộp tối đa)
            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        PurchaseOrderDetailId = 102,
                        ExpectedQuantity = 45,
                        AcceptedQuantity = 45,
                        RejectedQuantity = 0
                    }
                }
            };

            // Act & Assert: Phải chặn đứng over-receiving
            var act = () => service.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*vượt quá số lượng còn lại của đơn mua hàng*");
        }

        [Fact]
        public async Task CreateReceipt_WhenQuantityWithinPOTolerance_Succeeds()
        {
            // Arrange: PO đặt 100 hộp, đã nhận 60 hộp -> Còn lại 40 hộp.
            // Nhập 42 hộp (<= 44 hộp tối đa dung sai 10%) -> Hợp lệ.
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 13,
                OrderCode = "PO-VALID-TOLERANCE",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.PartiallyReceived,
                Details = new List<PurchaseOrderDetail>
                {
                    new() { Id = 103, VariantId = 1, UoMId = 1, OrderQuantity = 100, ReceivedQuantity = 60, UnitPrice = 80000m }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        PurchaseOrderDetailId = 103,
                        ExpectedQuantity = 42,
                        AcceptedQuantity = 42
                    }
                }
            };

            // Act
            var receiptId = await service.CreateAsync(createDto);

            // Assert
            receiptId.Should().BeGreaterThan(0);
        }

        #endregion

        #region NHÓM 2: KHIÊN BẢO VỆ TỒN KHO KHI ADMIN HỦY ĐƠN HÀNG (UNRESERVE SHIELD)

        [Fact]
        public async Task OrderService_UpdateStatusToCancelled_UnreservesStockCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var routingService = new OrderRoutingService(context, new DistanceService());

            // Tạo tồn kho ban đầu tại kho bán lẻ: Khả dụng = 50, Đang giữ chỗ = 20
            var inventory = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 2,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 20,
                QuantityDamaged = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.WarehouseInventories.Add(inventory);

            // Tạo đơn hàng ở trạng thái Confirmed đã giữ chỗ 20 đơn vị
            var order = new Order
            {
                Id = 50,
                OrderCode = "ORD-RESERVE-TEST",
                CustomerId = 1,
                WarehouseId = 2,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Unpaid,
                PaymentMethod = PaymentMethod.COD,
                SubTotal = 1600000m,
                TotalAmount = 1600000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<OrderDetail>
                {
                    new()
                    {
                        Id = 501,
                        VariantId = 1,
                        UoMId = 1,
                        Quantity = 20,
                        BaseQuantity = 20,
                        UnitPrice = 80000m,
                        TotalPrice = 1600000m,
                        IssuedQuantity = 0
                    }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var orderService = new OrderService(context, _mapper, routingService);

            // Act: Admin gọi UpdateStatusAsync để chuyển trạng thái sang Cancelled
            var result = await orderService.UpdateStatusAsync(50, new OrderUpdateDto
            {
                Status = OrderStatus.Cancelled,
                CancellationReason = "Khách hàng đổi ý không muốn nhận hàng"
            });

            // Assert
            result.Should().BeTrue();

            // 1. Trạng thái đơn hàng phải là Cancelled
            var updatedOrder = await context.Orders.FindAsync(50);
            updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);
            updatedOrder.CancellationReason.Should().Be("Khách hàng đổi ý không muốn nhận hàng");

            // 2. Tồn kho phải được giải phóng đúng chuẩn (Unreserve)
            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityReserved.Should().Be(0, "Hàng giữ chỗ phải được nhả về 0");
            updatedInv.QuantityAvailable.Should().Be(70, "Hàng khả dụng phải tăng từ 50 lên 70 (hoàn trả 20 đơn vị)");

            // 3. Sổ cái giao dịch kho phải ghi nhận giao dịch Unreserve
            var txn = await context.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ReferenceCode == "ORD-RESERVE-TEST" && t.Type == TransactionType.Unreserve);
            txn.Should().NotBeNull();
            txn!.Quantity.Should().Be(20);
        }

        #endregion

        #region NHÓM 3: KHIÊN BẢO VỆ ĐỔI TRẢ HÀNG KHÁCH HÀNG (CUSTOMER RMA SHIELDS)

        [Fact]
        public async Task CustomerReturn_CreateAsync_WhenOrderNotDeliveredOrCompleted_ThrowsInvalidOperationException()
        {
            // Arrange: Đơn hàng mới ở trạng thái Pending (chưa xuất kho / chưa giao)
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var order = new Order
            {
                Id = 60,
                OrderCode = "ORD-PENDING-RETURN",
                CustomerId = 1,
                WarehouseId = 2,
                Status = OrderStatus.Pending, // Mới đặt hàng, chưa giao
                TotalAmount = 500000m,
                Details = new List<OrderDetail>
                {
                    new() { Id = 601, VariantId = 1, UoMId = 1, Quantity = 5, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var returnService = new CustomerReturnService(context, _mapper);

            var createDto = new CustomerReturnCreateDto
            {
                OrderId = 60,
                CustomerId = 1,
                WarehouseId = 2,
                Details = new List<CustomerReturnDetailCreateDto>
                {
                    new() { VariantId = 1, UoMId = 1, ReturnedQuantity = 2 }
                }
            };

            // Act & Assert: Chặn lập RMA cho đơn chưa giao
            var act = () => returnService.CreateAsync(createDto, currentUserId: 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể lập phiếu trả hàng cho đơn hàng*");
        }

        [Fact]
        public async Task CustomerReturn_CreateAsync_WhenReturningMoreThanPurchased_ThrowsInvalidOperationException()
        {
            // Arrange: Đơn hàng Completed khách mua 2 hộp dâu
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var order = new Order
            {
                Id = 61,
                OrderCode = "ORD-OVER-RETURN",
                CustomerId = 1,
                WarehouseId = 2,
                Status = OrderStatus.Completed,
                TotalAmount = 200000m,
                Details = new List<OrderDetail>
                {
                    new() { Id = 602, VariantId = 1, UoMId = 1, Quantity = 2, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var returnService = new CustomerReturnService(context, _mapper);

            // Cố tình yêu cầu trả 5 hộp (> 2 hộp đã mua)
            var createDto = new CustomerReturnCreateDto
            {
                OrderId = 61,
                CustomerId = 1,
                WarehouseId = 2,
                Details = new List<CustomerReturnDetailCreateDto>
                {
                    new() { VariantId = 1, UoMId = 1, ReturnedQuantity = 5 }
                }
            };

            // Act & Assert: Phải chặn đứng hành vi trả hàng vượt số lượng mua
            var act = () => returnService.CreateAsync(createDto, currentUserId: 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*vượt quá số lượng mua còn lại có thể trả*");
        }

        [Fact]
        public async Task CustomerReturn_CreateAsync_WhenDoubleReturningAcrossRequests_ThrowsInvalidOperationException()
        {
            // Arrange: Đơn hàng Completed khách mua 2 hộp dâu.
            // Đã tạo 1 phiếu trả hàng cho 2 hộp dâu.
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var order = new Order
            {
                Id = 62,
                OrderCode = "ORD-DOUBLE-REFUND",
                CustomerId = 1,
                WarehouseId = 2,
                Status = OrderStatus.Completed,
                TotalAmount = 200000m,
                Details = new List<OrderDetail>
                {
                    new() { Id = 603, VariantId = 1, UoMId = 1, Quantity = 2, UnitPrice = 100000m }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var returnService = new CustomerReturnService(context, _mapper);

            // Phiếu trả 1: Trả 2 hộp -> Thành công
            var firstReturnDto = new CustomerReturnCreateDto
            {
                OrderId = 62,
                CustomerId = 1,
                WarehouseId = 2,
                Details = new List<CustomerReturnDetailCreateDto>
                {
                    new() { VariantId = 1, UoMId = 1, ReturnedQuantity = 2 }
                }
            };
            var retId1 = await returnService.CreateAsync(firstReturnDto, currentUserId: 1);
            retId1.Should().BeGreaterThan(0);

            // Phiếu trả 2: Tiếp tục tạo thêm phiếu trả 1 hộp nữa cho cùng đơn hàng
            var secondReturnDto = new CustomerReturnCreateDto
            {
                OrderId = 62,
                CustomerId = 1,
                WarehouseId = 2,
                Details = new List<CustomerReturnDetailCreateDto>
                {
                    new() { VariantId = 1, UoMId = 1, ReturnedQuantity = 1 }
                }
            };

            // Act & Assert: Phải chặn đứng Double-Refund
            var act = () => returnService.CreateAsync(secondReturnDto, currentUserId: 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*vượt quá số lượng mua còn lại có thể trả*đã lập phiếu trả trước đó: 2*");
        }

        #endregion
    }
}
