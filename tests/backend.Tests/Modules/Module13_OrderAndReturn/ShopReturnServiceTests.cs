using AutoMapper;
using backend.DTOs.ShopDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services;
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
    /// 🧪 UNIT TEST: ShopReturnService (B2C Self-Service RMA & Batch Resolution)
    /// ============================================================================
    /// </summary>
    public class ShopReturnServiceTests
    {
        private readonly IMapper _mapper;

        public ShopReturnServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: KHÁCH HÀNG TỰ TẠO YÊU CẦU ĐỔI TRẢ & TỰ ĐỘNG BÓC TÁCH BATCHID TỪ PHIẾU XUẤT KHO
        /// <summary>
        /// TC01: Kiểm tra hàm CreateReturnRequestAsync:
        /// 1. Tạo phiếu CustomerReturn ở trạng thái Pending.
        /// 2. Nếu khách không truyền BatchId, hệ thống tự động trích xuất BatchId từ InventoryIssueDetails đã Completed của đơn hàng.
        /// 3. Tính toán RefundAmount tạm tính chính xác.
        /// </summary>
        [Fact]
        public async Task CreateReturnRequestAsync_ShouldCreatePendingReturnWithAutoResolvedBatch()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var variant = new ProductVariant { Id = 10, Code = "SKU-DUA", Name = "Dưa Lưới Huỳnh Long", IsActive = true };
            var batch = new ProductBatch { Id = 8, BatchCode = "BATCH-DUA-08", VariantId = 10, ExpiryDate = now.AddDays(15) };
            var uom = new UoM { Id = 1, Code = "TRAI", Name = "Trái", IsActive = true };

            context.ProductVariants.Add(variant);
            context.ProductBatches.Add(batch);
            context.UoMs.Add(uom);

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-B2C-RETURN",
                CustomerId = 10,
                WarehouseId = 1,
                Status = OrderStatus.Completed, // Đã giao hàng
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, VariantId = 10, UoMId = 1, Quantity = 2, UnitPrice = 85000m, TotalPrice = 170000m }
                }
            };

            var completedIssue = new InventoryIssue
            {
                Id = 1,
                IssueCode = "PXK-COMPLETED",
                WarehouseId = 1,
                OrderId = 1,
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
                        BatchId = 8, // Lô hàng thực tế đã xuất
                        UoMId = 1,
                        Quantity = 2,
                        UnitPrice = 85000m,
                        TotalPrice = 170000m
                    }
                }
            };

            context.Orders.Add(order);
            context.InventoryIssues.Add(completedIssue);
            await context.SaveChangesAsync();

            var service = new ShopReturnService(context, _mapper);

            var request = new ShopReturnCreateRequestDto
            {
                OrderCode = "ORD-B2C-RETURN",
                Reason = "Quả dưa bị nứt và có mùi chua",
                Items = new List<ShopReturnItemRequestDto>
                {
                    new ShopReturnItemRequestDto
                    {
                        VariantId = 10,
                        BatchId = 0, // Khách không biết BatchId -> Hệ thống tự phân giải
                        UoMId = 1,
                        ReturnedQuantity = 1,
                        Reason = "Nứt quả"
                    }
                }
            };

            // Act
            var result = await service.CreateReturnRequestAsync(10, request);

            // Assert
            result.Should().NotBeNull();
            result.OrderCode.Should().Be("ORD-B2C-RETURN");
            result.Status.Should().Be(CustomerReturnStatus.Pending);
            result.StatusName.Should().Be("Chờ tiếp nhận");
            result.RefundAmount.Should().Be(85000m); // 1 trái * 85,000
            result.Reason.Should().Be("Quả dưa bị nứt và có mùi chua");

            result.Details.Should().HaveCount(1);
            var item = result.Details.First();
            item.VariantName.Should().Be("Dưa Lưới Huỳnh Long");
            item.BatchCode.Should().Be("BATCH-DUA-08"); // Đã tự động phân giải từ phiếu xuất kho!
            item.ReturnedQuantity.Should().Be(1);
            item.RefundAmount.Should().Be(85000m);
        }
        #endregion

        #region TC02: SHIELD CHẶN TRẢ HÀNG KHI ĐƠN HÀNG CHƯA GIAO
        /// <summary>
        /// TC02: Kiểm tra khi khách yêu cầu trả hàng cho đơn đang ở trạng thái Confirmed (chưa giao), hệ thống ném InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task CreateReturnRequestAsync_WhenOrderNotCompletedOrShipping_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-NOT-DELIVERED",
                CustomerId = 10,
                Status = OrderStatus.Confirmed, // Chưa giao
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new ShopReturnService(context, _mapper);

            var request = new ShopReturnCreateRequestDto
            {
                OrderCode = "ORD-NOT-DELIVERED",
                Items = new List<ShopReturnItemRequestDto>
                {
                    new ShopReturnItemRequestDto { VariantId = 1, UoMId = 1, ReturnedQuantity = 1 }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateReturnRequestAsync(10, request));
        }
        #endregion

        #region TC03: SHIELD TRẢ HÀNG VỚI ĐƠN HÀNG KHÔNG TỒN TẠI
        /// <summary>
        /// TC03: Kiểm tra yêu cầu trả hàng với OrderCode không tồn tại sẽ ném KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task CreateReturnRequestAsync_WhenOrderNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new ShopReturnService(context, _mapper);

            var request = new ShopReturnCreateRequestDto
            {
                OrderCode = "ORD-NON-EXISTENT",
                Items = new List<ShopReturnItemRequestDto>
                {
                    new ShopReturnItemRequestDto { VariantId = 1, UoMId = 1, ReturnedQuantity = 1 }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateReturnRequestAsync(10, request));
        }
        #endregion

        #region TC04: TRUY VẤN LỊCH SỬ ĐỔI TRẢ CỦA KHÁCH HÀNG (CUSTOMER SCOPED)
        /// <summary>
        /// TC04: Kiểm tra hàm GetCustomerReturnsAsync chỉ trả về danh sách phiếu đổi trả thuộc về customerId được chỉ định.
        /// </summary>
        [Fact]
        public async Task GetCustomerReturnsAsync_ShouldReturnCustomerReturnsPaged()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order1 = new Order { Id = 1, OrderCode = "ORD-U1", CustomerId = 10, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var order2 = new Order { Id = 2, OrderCode = "ORD-U2", CustomerId = 20, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var retUser1 = new CustomerReturn { Id = 1, ReturnCode = "RET-U1", OrderId = 1, CustomerId = 10, ReturnDate = now, CreatedAt = now, UpdatedAt = now };
            var retUser2 = new CustomerReturn { Id = 2, ReturnCode = "RET-U2", OrderId = 2, CustomerId = 20, ReturnDate = now, CreatedAt = now, UpdatedAt = now };

            context.Orders.AddRange(order1, order2);
            context.CustomerReturns.AddRange(retUser1, retUser2);
            await context.SaveChangesAsync();

            var service = new ShopReturnService(context, _mapper);

            // Act
            var result = await service.GetCustomerReturnsAsync(10, pageIndex: 1, pageSize: 10);

            // Assert
            result.TotalRecords.Should().Be(1);
            result.Items.First().ReturnCode.Should().Be("RET-U1");
        }
        #endregion

        #region TC05: TRUY VẤN CHI TIẾT PHIẾU ĐỔI TRẢ THEO MÃ CÔNG KHAI (RETURNCODE)
        /// <summary>
        /// TC05: Kiểm tra hàm GetReturnByCodeAsync trả về đúng phiếu đổi trả theo returnCode và customerId.
        /// </summary>
        [Fact]
        public async Task GetReturnByCodeAsync_ShouldReturnReturnDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order { Id = 1, OrderCode = "ORD-ORDER-999", CustomerId = 10, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var variant = new ProductVariant { Id = 10, Code = "SKU-MAN", Name = "Mận Hậu", IsActive = true };
            var batch = new ProductBatch { Id = 1, BatchCode = "BATCH-MAN-01", VariantId = 10, ExpiryDate = now.AddDays(10) };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kg", IsActive = true };

            var ret = new CustomerReturn
            {
                ReturnCode = "RET-PUBLIC-999",
                OrderId = 1,
                CustomerId = 10,
                Status = CustomerReturnStatus.Completed,
                RefundAmount = 120000m,
                InspectionNotes = "Đã hoàn tất chuyển khoản",
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            ret.Details.Add(new CustomerReturnDetail
            {
                VariantId = 10,
                BatchId = 1,
                UoMId = 1,
                ReturnedQuantity = 2,
                AcceptedQuantity = 2,
                DamagedQuantity = 0,
                RefundAmount = 120000m
            });

            context.Orders.Add(order);
            context.ProductVariants.Add(variant);
            context.ProductBatches.Add(batch);
            context.UoMs.Add(uom);
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new ShopReturnService(context, _mapper);

            // Act
            var result = await service.GetReturnByCodeAsync(10, "RET-PUBLIC-999");

            // Assert
            result.Should().NotBeNull();
            result!.ReturnCode.Should().Be("RET-PUBLIC-999");
            result.StatusName.Should().Be("Đã hoàn tất & hoàn tiền");
            result.RefundAmount.Should().Be(120000m);
            result.Details.Should().HaveCount(1);
            result.Details.First().VariantName.Should().Be("Mận Hậu");
        }
        #endregion

        #region TC06: SHIELD CHẶN TRẢ HÀNG KHI ĐƠN HÀNG QUÁ THỜI HẠN 12 GIỜ (NÔNG SẢN TƯƠI SỐNG)
        /// <summary>
        /// TC06: Kiểm tra chính sách nông sản tươi sống - Chặn tạo phiếu đổi trả nếu đơn hàng đã đặt quá 12 giờ.
        /// </summary>
        [Fact]
        public async Task CreateReturnRequestAsync_WhenOrderExceeds12Hours_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-EXPIRED-12H",
                CustomerId = 10,
                Status = OrderStatus.Completed,
                OrderDate = now.AddHours(-13), // Đã đặt 13 tiếng trước (> 12h)
                CreatedAt = now.AddHours(-13),
                UpdatedAt = now.AddHours(-1)
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new ShopReturnService(context, _mapper);

            var request = new ShopReturnCreateRequestDto
            {
                OrderCode = "ORD-EXPIRED-12H",
                Reason = "Hàng dập",
                Items = new List<ShopReturnItemRequestDto>
                {
                    new() { VariantId = 1, UoMId = 1, ReturnedQuantity = 1 }
                }
            };

            // Act & Assert
            var act = async () => await service.CreateReturnRequestAsync(10, request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*quá thời hạn 12 giờ*");
        }
        #endregion

        #region TC07: SHIELD CHẶN TẠO YÊU CẦU TRẢ HÀNG VỚI DANH SÁCH SẢN PHẨM RỖNG
        /// <summary>
        /// TC07: Kiểm tra khi request.Items rỗng sẽ ném ArgumentException yêu cầu chọn ít nhất 1 sản phẩm.
        /// </summary>
        [Fact]
        public async Task CreateReturnRequestAsync_WhenItemsEmpty_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-EMPTY-ITEMS",
                CustomerId = 10,
                Status = OrderStatus.Completed,
                OrderDate = now.AddHours(-2), // Trong vòng 12h
                CreatedAt = now.AddHours(-2),
                UpdatedAt = now
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new ShopReturnService(context, _mapper);

            var request = new ShopReturnCreateRequestDto
            {
                OrderCode = "ORD-EMPTY-ITEMS",
                Reason = "Không ưng",
                Items = new List<ShopReturnItemRequestDto>()
            };

            // Act & Assert
            var act = async () => await service.CreateReturnRequestAsync(10, request);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*ít nhất một sản phẩm*");
        }
        #endregion
    }
}
