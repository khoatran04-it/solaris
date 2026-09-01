using AutoMapper;
using backend.DTOs.CustomerReturnDTOs;
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
    /// 🧪 UNIT TEST: CustomerReturnService (Quản Lý Trả Hàng, Nghiệm Thu QC & Điều Hướng Tồn Kho)
    /// ============================================================================
    /// </summary>
    public class CustomerReturnServiceTests
    {
        private readonly IMapper _mapper;

        public CustomerReturnServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: TRUY VẤN DANH SÁCH PHIẾU TRẢ HÀNG PHÂN TRANG VÀ LÀM PHẲNG DỮ LIỆU
        /// <summary>
        /// TC01: Kiểm tra hàm GetPagedAsync lọc theo từ khóa, phân trang và làm phẳng dữ liệu
        /// (OrderCode, CustomerName, WarehouseName, ReceivedByName) chính xác.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldReturnPagedReturnsWithFlattenedFields()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Nguyễn Văn A", PhoneNumber = "0901234567", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "Kho Tổng", IsActive = true };
            var user = new IAUser
            {
                Id = 1,
                CitizenId = "001200000002",
                Username = "qc_user",
                FullName = "Nhân viên QC",
                Email = "qc@solaris.vn",
                PhoneNumber = "0902223333",
                PasswordHash = "hash",
                IsActive = true
            };
            var order = new Order { Id = 10, OrderCode = "ORD-ORIGINAL-01", CustomerId = 1, WarehouseId = 1, OrderDate = now, CreatedAt = now, UpdatedAt = now };

            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            context.IAUsers.Add(user);
            context.Orders.Add(order);

            var ret1 = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-20260830-001",
                OrderId = 10,
                CustomerId = 1,
                WarehouseId = 1,
                ReceivedById = 1,
                Status = CustomerReturnStatus.Pending,
                RefundAmount = 0m,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            var ret2 = new CustomerReturn
            {
                Id = 2,
                ReturnCode = "RET-20260830-002",
                OrderId = 10,
                CustomerId = 1,
                WarehouseId = 1,
                ReceivedById = 1,
                Status = CustomerReturnStatus.Completed,
                RefundAmount = 150000m,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.CustomerReturns.AddRange(ret1, ret2);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act
            var result = await service.GetPagedAsync(
                search: "001",
                warehouseId: null,
                status: null,
                startDate: null,
                endDate: null,
                pageIndex: 1,
                pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(1);
            var item = result.Items.First();
            item.ReturnCode.Should().Be("RET-20260830-001");
            item.OrderCode.Should().Be("ORD-ORIGINAL-01");
            item.CustomerName.Should().Be("Nguyễn Văn A");
            item.WarehouseName.Should().Be("Kho Tổng");
            item.ReceivedByName.Should().Be("Nhân viên QC");
        }
        #endregion

        #region TC02: PHÂN QUYỀN TRUY CẬP KHO (DATA ISOLATION) TRONG DANH SÁCH TRẢ HÀNG
        /// <summary>
        /// TC02: Kiểm tra tham số allowedWarehouseIds chỉ cho phép xem phiếu trả của các kho được phân quyền.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_WithAllowedWarehouseIds_ShouldEnforceDataIsolation()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Khách Hàng", PhoneNumber = "0901112222", IsActive = true };
            var wh1 = new Warehouse { Id = 1, Code = "WH-1", Name = "Kho 1", IsActive = true };
            var wh2 = new Warehouse { Id = 2, Code = "WH-2", Name = "Kho 2", IsActive = true };
            var order1 = new Order { Id = 1, OrderCode = "ORD-1", CustomerId = 1, WarehouseId = 1, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var order2 = new Order { Id = 2, OrderCode = "ORD-2", CustomerId = 1, WarehouseId = 2, OrderDate = now, CreatedAt = now, UpdatedAt = now };

            var retWh1 = new CustomerReturn { Id = 1, ReturnCode = "RET-WH1", OrderId = 1, CustomerId = 1, WarehouseId = 1, ReturnDate = now, CreatedAt = now, UpdatedAt = now };
            var retWh2 = new CustomerReturn { Id = 2, ReturnCode = "RET-WH2", OrderId = 2, CustomerId = 1, WarehouseId = 2, ReturnDate = now, CreatedAt = now, UpdatedAt = now };

            context.Customers.Add(customer);
            context.Warehouses.AddRange(wh1, wh2);
            context.Orders.AddRange(order1, order2);
            context.CustomerReturns.AddRange(retWh1, retWh2);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act: Chỉ có quyền xem Kho 2
            var result = await service.GetPagedAsync(
                search: null, warehouseId: null, status: null, startDate: null, endDate: null,
                pageIndex: 1, pageSize: 10,
                allowedWarehouseIds: new List<int> { 2 });

            // Assert
            result.TotalRecords.Should().Be(1);
            result.Items.First().ReturnCode.Should().Be("RET-WH2");
        }
        #endregion

        #region TC03: TRUY VẤN CHI TIẾT PHIẾU TRẢ HÀNG KÈM THÔNG TIN LÔ HÀNG
        /// <summary>
        /// TC03: Kiểm tra hàm GetByIdAsync trả về chi tiết phiếu trả và làm phẳng BatchCode, VariantCode, UoMName.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnReturnWithDetailsAndBatchInfo()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order { Id = 1, OrderCode = "ORD-ORIGINAL", CustomerId = 1, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Khách Hàng", PhoneNumber = "0901112222", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "Kho Tổng", IsActive = true };
            var variant = new ProductVariant { Id = 10, Code = "SKU-BO", Name = "Bơ 034", IsActive = true };
            var batch = new ProductBatch { Id = 3, BatchCode = "BATCH-BO-003", VariantId = 10, ExpiryDate = now.AddDays(10) };
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true };

            context.Orders.Add(order);
            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            context.ProductVariants.Add(variant);
            context.ProductBatches.Add(batch);
            context.UoMs.Add(uom);

            var ret = new CustomerReturn
            {
                Id = 100,
                ReturnCode = "RET-DETAIL-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<CustomerReturnDetail>
                {
                    new CustomerReturnDetail
                    {
                        Id = 1,
                        VariantId = 10,
                        BatchId = 3,
                        UoMId = 1,
                        ReturnedQuantity = 3,
                        UnitPrice = 60000m,
                        AcceptedQuantity = 2,
                        DamagedQuantity = 1,
                        RefundAmount = 180000m,
                        RejectReason = "1kg bị thối cuống"
                    }
                }
            };
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(100);

            // Assert
            result.Should().NotBeNull();
            result.ReturnCode.Should().Be("RET-DETAIL-001");
            result.Details.Should().HaveCount(1);
            var detail = result.Details.First();
            detail.VariantName.Should().Be("Bơ 034");
            detail.BatchCode.Should().Be("BATCH-BO-003");
            detail.UoMName.Should().Be("Kilogram");
            detail.AcceptedQuantity.Should().Be(2);
            detail.DamagedQuantity.Should().Be(1);
            detail.RejectReason.Should().Be("1kg bị thối cuống");
        }
        #endregion

        #region TC04: KHỞI TẠO YÊU CẦU TRẢ HÀNG (RMA INITIATION)
        /// <summary>
        /// TC04: Kiểm tra hàm CreateAsync tạo phiếu trả hàng ở trạng thái Pending và KHÔNG làm thay đổi tồn kho vật lý.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCreatePendingReturnWithoutMutatingInventory()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order { Id = 1, OrderCode = "ORD-001", CustomerId = 1, WarehouseId = 1, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var customer = new Customer { Id = 1, Code = "CUST-001", Name = "Khách Hàng A", PhoneNumber = "0901234567", IsActive = true };
            var warehouse = new Warehouse { Id = 1, Code = "WH-01", Name = "Kho", IsActive = true };

            context.Orders.Add(order);
            context.Customers.Add(customer);
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            var createDto = new CustomerReturnCreateDto
            {
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Reason = "Khách bảo sản phẩm không đúng chất lượng",
                Details = new List<CustomerReturnDetailCreateDto>
                {
                    new CustomerReturnDetailCreateDto
                    {
                        VariantId = 10,
                        BatchId = 1,
                        UoMId = 1,
                        ReturnedQuantity = 2,
                        UnitPrice = 50000m
                    }
                }
            };

            // Act
            int returnId = await service.CreateAsync(createDto, currentUserId: 1);

            // Assert
            returnId.Should().BeGreaterThan(0);

            var created = await context.CustomerReturns.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == returnId);
            created.Should().NotBeNull();
            created!.Status.Should().Be(CustomerReturnStatus.Pending);
            created.RefundAmount.Should().Be(0m); // Chưa nghiệm thu nên tiền hoàn = 0
            created.Details.Should().HaveCount(1);
            created.Details.First().ReturnedQuantity.Should().Be(2);
            created.Details.First().AcceptedQuantity.Should().Be(0);

            // Sổ cái không phát sinh giao dịch tồn kho
            var txns = await context.InventoryTransactions.ToListAsync();
            txns.Should().BeEmpty();
        }
        #endregion

        #region TC05: SHIELD KIỂM TRA ĐƠN HÀNG GỐC KHÔNG TỒN TẠI
        /// <summary>
        /// TC05: Kiểm tra tạo phiếu trả hàng với OrderId không tồn tại sẽ ném ArgumentException.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithInvalidOrder_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new CustomerReturnService(context, _mapper);

            var createDto = new CustomerReturnCreateDto
            {
                OrderId = 999,
                Details = new List<CustomerReturnDetailCreateDto>
                {
                    new CustomerReturnDetailCreateDto { VariantId = 1, BatchId = 1, UoMId = 1, ReturnedQuantity = 1 }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(createDto));
        }
        #endregion

        #region TC06: KIỂM ĐỊNH QC & ĐIỀU HƯỚNG TỒN KHO 2 NGĂN (AVAILABLE VS DAMAGED)
        /// <summary>
        /// TC06: Kiểm tra hàm InspectAndCompleteAsync nghiệm thu QC:
        /// 1. Cộng AcceptedQuantity vào QuantityAvailable (+ Available để bán tiếp).
        /// 2. Cộng DamagedQuantity vào QuantityDamaged (+ Damaged hàng hỏng).
        /// 3. Tính toán RefundAmount chính xác và cập nhật Order.PaymentStatus = Refunded.
        /// 4. Ghi sổ cái InventoryTransaction với Type = CustomerReturn.
        /// </summary>
        [Fact]
        public async Task InspectAndCompleteAsync_ShouldRouteStockToAvailableAndDamagedBuckets()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser
            {
                Id = 1,
                CitizenId = "001200000003",
                Username = "qc_inspector",
                FullName = "KCS Kho",
                Email = "kcs@solaris.vn",
                PhoneNumber = "0903334444",
                PasswordHash = "hash",
                IsActive = true
            };
            var order = new Order { Id = 1, OrderCode = "ORD-REFUND", CustomerId = 1, WarehouseId = 1, PaymentStatus = PaymentStatus.Paid, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var initialInv = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 10,
                BatchId = 1,
                QuantityAvailable = 10,
                QuantityDamaged = 0,
                QuantityReserved = 0
            };

            var customerReturn = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-QC-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Pending,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<CustomerReturnDetail>
                {
                    new CustomerReturnDetail
                    {
                        Id = 1,
                        CustomerReturnId = 1,
                        VariantId = 10,
                        BatchId = 1,
                        UoMId = 1,
                        ReturnedQuantity = 5,
                        UnitPrice = 40000m
                    }
                }
            };

            context.IAUsers.Add(user);
            context.Orders.Add(order);
            context.WarehouseInventories.Add(initialInv);
            context.CustomerReturns.Add(customerReturn);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            var inspectionDto = new CustomerReturnInspectionDto
            {
                InspectionNotes = "Kiểm tra 5kg: 3kg đạt chuẩn nhập lại kho bán, 2kg dập nát hủy bỏ",
                Items = new List<CustomerReturnItemInspectionDto>
                {
                    new CustomerReturnItemInspectionDto
                    {
                        DetailId = 1,
                        AcceptedQuantity = 3,
                        DamagedQuantity = 2,
                        RejectReason = "2kg dập nát do vận chuyển"
                    }
                }
            };

            // Act
            bool result = await service.InspectAndCompleteAsync(1, receivedById: 1, inspectionDto);

            // Assert
            result.Should().BeTrue();

            var completedReturn = await context.CustomerReturns.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == 1);
            completedReturn!.Status.Should().Be(CustomerReturnStatus.Completed);
            completedReturn.RefundAmount.Should().Be(200000m); // (3 + 2) * 40,000 = 200,000

            // Kiểm tra tồn kho 2 ngăn
            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityAvailable.Should().Be(13); // 10 + 3 = 13
            updatedInv.QuantityDamaged.Should().Be(2);   // 0 + 2 = 2

            // Kiểm tra trạng thái thanh toán đơn gốc
            var updatedOrder = await context.Orders.FindAsync(1);
            updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.Refunded);

            // Kiểm tra sổ cái bất biến đã ghi nhận
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "RET-QC-001");
            txn.Should().NotBeNull();
            txn!.Type.Should().Be(TransactionType.CustomerReturn);
            txn.Quantity.Should().Be(5); // Tổng thu hồi 5
        }
        #endregion

        #region TC07: SHIELD CHẶN NGHIỆM THU PHIẾU ĐÃ HOÀN TẤT
        /// <summary>
        /// TC07: Kiểm tra phiếu trả hàng đã ở trạng thái Completed sẽ không được phép nghiệm thu lại.
        /// </summary>
        [Fact]
        public async Task InspectAndCompleteAsync_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order { Id = 1, OrderCode = "ORD-COMPLETED-01", CustomerId = 1, WarehouseId = 1, OrderDate = now, CreatedAt = now, UpdatedAt = now };
            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-COMPLETED",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Completed,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Orders.Add(order);
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            var inspectionDto = new CustomerReturnInspectionDto
            {
                Items = new List<CustomerReturnItemInspectionDto>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.InspectAndCompleteAsync(1, 1, inspectionDto));
        }
        #endregion

        #region TC08: TỪ CHỐI TIẾP NHẬN PHIẾU TRẢ HÀNG
        /// <summary>
        /// TC08: Kiểm tra hàm RejectReturnAsync chuyển trạng thái sang Rejected và ghi nhận lý do từ chối.
        /// </summary>
        [Fact]
        public async Task RejectReturnAsync_ShouldSetStatusToRejectedWithoutStockMutation()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-REJECT",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Pending,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act
            bool result = await service.RejectReturnAsync(1, "Hàng đã quá hạn đổi trả 7 ngày theo quy định");

            // Assert
            result.Should().BeTrue();
            var updated = await context.CustomerReturns.FindAsync(1);
            updated!.Status.Should().Be(CustomerReturnStatus.Rejected);
            updated.InspectionNotes.Should().Contain("Hàng đã quá hạn đổi trả");

            // Không phát sinh giao dịch tồn kho
            var txns = await context.InventoryTransactions.ToListAsync();
            txns.Should().BeEmpty();
        }
        #endregion

        #region TC09: SHIELD CHẶN XÓA PHIẾU TRẢ HÀNG ĐÃ HOÀN TẤT
        /// <summary>
        /// TC09: Kiểm tra hàm DeleteAsync chặn xóa mềm đối với các phiếu trả hàng đã Completed để bảo toàn sổ cái.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-CANNOT-DELETE",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Completed,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1));
        }
        #endregion

        #region TC10: XÓA MỀM PHIẾU TRẢ HÀNG ĐANG CHỜ XỬ LÝ (PENDING)
        /// <summary>
        /// TC10: Kiểm tra hàm DeleteAsync xóa mềm thành công đối với phiếu trả hàng ở trạng thái Pending.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenPending_ShouldSoftDelete()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-PENDING-DEL",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Pending,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act
            bool result = await service.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            var deleted = await context.CustomerReturns.FindAsync(1);
            deleted!.IsDeleted.Should().BeTrue();
            deleted.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC11: DUYỆT PHIẾU TRẢ HÀNG (PENDING -> APPROVED)
        /// <summary>
        /// TC11: Kiểm tra hàm ApproveAsync chuyển trạng thái từ Pending sang Approved và gán Người duyệt.
        /// </summary>
        [Fact]
        public async Task ApproveAsync_WhenPending_ShouldSetStatusToApproved()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser
            {
                Id = 5,
                CitizenId = "001200000005",
                Username = "approver",
                FullName = "Quản lý kho",
                Email = "manager@solaris.vn",
                PhoneNumber = "0908887777",
                PasswordHash = "hash",
                IsActive = true
            };

            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-APPROVE-01",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Pending,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.IAUsers.Add(user);
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act
            bool result = await service.ApproveAsync(1, approvedById: 5);

            // Assert
            result.Should().BeTrue();
            var updated = await context.CustomerReturns.FindAsync(1);
            updated!.Status.Should().Be(CustomerReturnStatus.Approved);
            updated.ReceivedById.Should().Be(5);
        }
        #endregion

        #region TC12: SHIELD CHẶN DUYỆT PHIẾU KHÔNG Ở TRẠNG THÁI PENDING
        /// <summary>
        /// TC12: Kiểm tra hàm ApproveAsync ném InvalidOperationException nếu phiếu không ở trạng thái Pending.
        /// </summary>
        [Fact]
        public async Task ApproveAsync_WhenNotPending_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-ALREADY-APPROVED",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Approved,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act & Assert
            var act = async () => await service.ApproveAsync(1, 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ có thể duyệt Phiếu trả hàng đang ở trạng thái Chờ tiếp nhận*");
        }
        #endregion

        #region TC13: BƯỚC KIỂM ĐỊNH QC TẠI KHO (APPROVED -> INSPECTING)
        /// <summary>
        /// TC13: Kiểm tra hàm InspectQCAsync phân loại hàng tốt (Accepted) và hàng hỏng (Damaged),
        /// chuyển trạng thái sang Inspecting và tính RefundAmount, nhưng CHƯA làm thay đổi tồn kho.
        /// </summary>
        [Fact]
        public async Task InspectQCAsync_ShouldClassifyQuantitiesAndSetStatusToInspecting()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var user = new IAUser
            {
                Id = 2,
                CitizenId = "001200000006",
                Username = "qc_worker",
                FullName = "KCS Kho A",
                Email = "kcsA@solaris.vn",
                PhoneNumber = "0907778888",
                PasswordHash = "hash",
                IsActive = true
            };

            var ret = new CustomerReturn
            {
                Id = 10,
                ReturnCode = "RET-QC-STEP",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Approved,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<CustomerReturnDetail>
                {
                    new CustomerReturnDetail
                    {
                        Id = 101,
                        CustomerReturnId = 10,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ReturnedQuantity = 10,
                        UnitPrice = 50000m
                    }
                }
            };

            context.IAUsers.Add(user);
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            var inspectionDto = new CustomerReturnInspectionDto
            {
                InspectionNotes = "Nghiệm thu: 7kg đạt chuẩn, 3kg thối hỏng do nhiệt độ",
                Items = new List<CustomerReturnItemInspectionDto>
                {
                    new CustomerReturnItemInspectionDto
                    {
                        DetailId = 101,
                        AcceptedQuantity = 7,
                        DamagedQuantity = 3,
                        RejectReason = "3kg thối hỏng"
                    }
                }
            };

            // Act
            bool result = await service.InspectQCAsync(10, receivedById: 2, inspectionDto);

            // Assert
            result.Should().BeTrue();

            var updated = await context.CustomerReturns.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == 10);
            updated!.Status.Should().Be(CustomerReturnStatus.Inspecting);
            updated.RefundAmount.Should().Be(500000m); // (7 + 3) * 50,000 = 500,000
            updated.InspectionNotes.Should().Be("Nghiệm thu: 7kg đạt chuẩn, 3kg thối hỏng do nhiệt độ");

            var detail = updated.Details.First();
            detail.AcceptedQuantity.Should().Be(7);
            detail.DamagedQuantity.Should().Be(3);

            // Tồn kho chưa thay đổi
            var txns = await context.InventoryTransactions.ToListAsync();
            txns.Should().BeEmpty();
        }
        #endregion

        #region TC14: HOÀN TẤT PHIẾU TRẢ HÀNG & GIẢI PHÓNG GIỮ CHỖ (COMPLETE RETURN)
        /// <summary>
        /// TC14: Kiểm tra hàm CompleteReturnAsync:
        /// 1. Cập nhật tồn kho (QuantityAvailable += 6, QuantityDamaged += 4).
        /// 2. Giải phóng giữ chỗ tồn kho (QuantityReserved) của đơn hàng gốc nếu đơn chưa từng xuất kho (Confirmed/Processing).
        /// 3. Ghi sổ cái InventoryTransaction với Type = CustomerReturn.
        /// 4. Đổi Order.PaymentStatus = Refunded và Return.Status = Completed.
        /// </summary>
        [Fact]
        public async Task CompleteReturnAsync_ShouldUpdateInventory_ReleaseReservedStock_AndLogLedger()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-WITH-RESERVE",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                OrderDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            var inventory = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 10,
                BatchId = 1,
                QuantityAvailable = 20,
                QuantityReserved = 10, // Đang bị giữ chỗ 10
                QuantityDamaged = 0
            };

            var ret = new CustomerReturn
            {
                Id = 1,
                ReturnCode = "RET-COMPLETE-TEST",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                ReceivedById = 1,
                Status = CustomerReturnStatus.Inspecting,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now,
                Details = new List<CustomerReturnDetail>
                {
                    new CustomerReturnDetail
                    {
                        Id = 1,
                        CustomerReturnId = 1,
                        VariantId = 10,
                        BatchId = 1,
                        UoMId = 1,
                        ReturnedQuantity = 10,
                        UnitPrice = 30000m,
                        AcceptedQuantity = 6,
                        DamagedQuantity = 4,
                        RefundAmount = 300000m
                    }
                }
            };

            context.Orders.Add(order);
            context.WarehouseInventories.Add(inventory);
            context.CustomerReturns.Add(ret);
            await context.SaveChangesAsync();

            var service = new CustomerReturnService(context, _mapper);

            // Act
            bool result = await service.CompleteReturnAsync(1);

            // Assert
            result.Should().BeTrue();

            // 1. Kiểm tra tồn kho
            var updatedInv = await context.WarehouseInventories.FindAsync(1);
            updatedInv!.QuantityAvailable.Should().Be(26); // 20 + 6 (Accepted) = 26
            updatedInv.QuantityDamaged.Should().Be(4);     // 0 + 4 (Damaged) = 4
            updatedInv.QuantityReserved.Should().Be(0);   // Đã giải phóng giữ chỗ 10 - 10 = 0

            // 2. Kiểm tra sổ cái bất biến
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "RET-COMPLETE-TEST");
            txn.Should().NotBeNull();
            txn!.Type.Should().Be(TransactionType.CustomerReturn);
            txn.Quantity.Should().Be(10); // 6 + 4 = 10

            // 3. Kiểm tra Order & Return
            var updatedOrder = await context.Orders.FindAsync(1);
            updatedOrder!.PaymentStatus.Should().Be(PaymentStatus.Refunded);

            var updatedRet = await context.CustomerReturns.FindAsync(1);
            updatedRet!.Status.Should().Be(CustomerReturnStatus.Completed);
        }
        #endregion

        #region TC15: SHIELD HOÀN TẤT PHIẾU KHÔNG TỒN TẠI
        /// <summary>
        /// TC15: Kiểm tra gọi CompleteReturnAsync với ID không tồn tại sẽ ném KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task CompleteReturnAsync_WhenNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new CustomerReturnService(context, _mapper);

            // Act & Assert
            var act = async () => await service.CompleteReturnAsync(999);
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy Phiếu trả hàng*");
        }
        #endregion
    }
}
