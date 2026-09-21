using AutoMapper;
using backend.Data;
using backend.DTOs.VehicleDTOs;
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

namespace backend.Tests.Modules.Module14_PaymentAndShipping
{
    /// <summary>
    /// ============================================================================
    /// MODULE 14: DELIVERY TRIPS & LOGISTICS FLEET SHIELDS
    /// UNIT TESTS: DeliveryTripShieldTests (3 Luồng: Giao B2C, Đơn trả RMA, Chuyển kho B2B)
    /// ĐẶC BIỆT TUÂN THỦ: Quy tắc FEFO (First Expired, First Out) cho nông sản tươi & chuỗi lạnh
    /// ============================================================================
    /// </summary>
    public class DeliveryTripShieldTests
    {
        private readonly IMapper _mapper;

        public DeliveryTripShieldTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Seed
        private static async Task SeedBasicDataAsync(SolarisDbContext context)
        {
            // Kho hàng
            var wh1 = new Warehouse { Id = 1, Code = "WH-HCM-01", Name = "Tổng Kho TP.HCM", IsActive = true };
            var wh2 = new Warehouse { Id = 2, Code = "WH-HN-01", Name = "Tổng Kho Hà Nội", IsActive = true };
            context.Warehouses.AddRange(wh1, wh2);

            // Danh mục sản phẩm
            var catAmbient = new ProductCategory { Id = 1, Code = "CAT-DRY", Name = "Nông sản khô", RequiresColdChain = false, IsActive = true };
            var catCold = new ProductCategory { Id = 2, Code = "CAT-COLD", Name = "Nông sản tươi lạnh", RequiresColdChain = true, IsActive = true };
            context.ProductCategories.AddRange(catAmbient, catCold);

            // Đơn vị tính
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true };
            context.UoMs.Add(uom);

            // Sản phẩm & Biến thể
            var pAmbient = new Product { Id = 1, Code = "PROD-RICE", Name = "Gạo ST25", CategoryId = 1, BaseUoMId = 1, IsActive = true };
            var pCold = new Product { Id = 2, Code = "PROD-BERRY", Name = "Dâu Tây Đà Lạt", CategoryId = 2, BaseUoMId = 1, IsActive = true };
            context.Products.AddRange(pAmbient, pCold);

            var vAmbient = new ProductVariant { Id = 1, ProductId = 1, Code = "SKU-RICE-5KG", Name = "Gạo ST25 túi 5kg", IsActive = true };
            var vCold = new ProductVariant { Id = 2, ProductId = 2, Code = "SKU-BERRY-BOX", Name = "Dâu Tây Hộp 500g", IsActive = true };
            context.ProductVariants.AddRange(vAmbient, vCold);

            // Lô hàng cho quy tắc FEFO (First Expired, First Out)
            // Lô 1: Hết hạn sau 30 ngày (Xa hơn)
            // Lô 2: Hết hạn sau 3 ngày (Cận date nhất -> FEFO phải ưu tiên chọn lô này trước)
            var batchFar = new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-COLD-EXP30D",
                VariantId = 2,
                ManufactureDate = DateTime.UtcNow.AddDays(-5),
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };
            var batchNear = new ProductBatch
            {
                Id = 2,
                BatchCode = "BATCH-COLD-EXP03D",
                VariantId = 2,
                ManufactureDate = DateTime.UtcNow.AddDays(-10),
                ExpiryDate = DateTime.UtcNow.AddDays(3),
                IsActive = true
            };
            context.ProductBatches.AddRange(batchFar, batchNear);

            // Khách hàng
            var customer = new Customer
            {
                Id = 1,
                Code = "CUST-001",
                Name = "Khách Hàng Solaris",
                PhoneNumber = "0901234567",
                IsActive = true
            };
            context.Customers.Add(customer);

            // Phương tiện vận chuyển
            // 1. Xe máy thường (không có thùng lạnh)
            var motorbikeNormal = new DeliveryVehicle
            {
                Id = 1,
                Code = "VEH-MOTO-NORM",
                LicensePlate = "59-X1 111.11",
                VehicleType = "Motorbike",
                MaxWeightKg = 30,
                IsColdChainEquipped = false,
                Status = "Available",
                DriverName = "Nguyễn Văn Thường",
                DriverPhone = "0911111111",
                HomeWarehouseId = 1,
                IsActive = true
            };

            // 2. Xe máy thùng lạnh (có thùng lạnh)
            var motorbikeCold = new DeliveryVehicle
            {
                Id = 2,
                Code = "VEH-MOTO-COLD",
                LicensePlate = "59-X1 222.22",
                VehicleType = "Motorbike",
                MaxWeightKg = 30,
                IsColdChainEquipped = true,
                Status = "Available",
                DriverName = "Trần Văn Lạnh",
                DriverPhone = "0922222222",
                HomeWarehouseId = 1,
                IsActive = true
            };

            // 3. Xe tải lạnh chuyên dụng (RefrigeratedTruck)
            var truckCold = new DeliveryVehicle
            {
                Id = 3,
                Code = "VEH-TRUCK-COLD",
                LicensePlate = "50H-999.99",
                VehicleType = "RefrigeratedTruck",
                MaxWeightKg = 2500,
                IsColdChainEquipped = true,
                Status = "Available",
                DriverName = "Lê Văn Tải",
                DriverPhone = "0933333333",
                HomeWarehouseId = 1,
                IsActive = true
            };

            // Tồn kho kho 1 cho Variant 2, Batch 2
            var inv1 = new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 2,
                BatchId = 2,
                QuantityAvailable = 100,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.WarehouseInventories.Add(inv1);

            context.DeliveryVehicles.AddRange(motorbikeNormal, motorbikeCold, truckCold);
            await context.SaveChangesAsync();
        }
        #endregion

        #region LUỒNG 1: GIAO HÀNG B2C (OUTBOUND DELIVERY)

        [Fact]
        public async Task TC01_CreateTrip_WhenVehicleInactive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var vehicle = await context.DeliveryVehicles.FindAsync(1);
            vehicle!.IsActive = false;
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var dto = new DeliveryTripCreateDto
            {
                VehicleId = 1,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int>()
            };

            // Act & Assert
            var act = async () => await service.CreateTripAsync(dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ngưng hoạt động*");
        }

        [Fact]
        public async Task TC02_CreateTrip_WhenVehicleMaintenanceOrOnTrip_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var vehicle = await context.DeliveryVehicles.FindAsync(1);
            vehicle!.Status = "Maintenance";
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var dto = new DeliveryTripCreateDto
            {
                VehicleId = 1,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int>()
            };

            // Act & Assert (Maintenance)
            var act = async () => await service.CreateTripAsync(dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*bảo dưỡng*");

            // Đổi sang OnTrip
            vehicle.Status = "OnTrip";
            await context.SaveChangesAsync();

            var actOnTrip = async () => await service.CreateTripAsync(dto);
            await actOnTrip.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*chuyến xe khác*");
        }

        [Fact]
        public async Task TC03_CreateTrip_WhenColdChainOrderAssignedToNormalVehicle_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            // Tạo đơn hàng chứa Dâu Tây (RequiresColdChain = true)
            var coldOrder = new Order
            {
                Id = 101,
                OrderCode = "ORD-COLD-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed,
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        VariantId = 2, // Dâu Tây
                        UoMId = 1,
                        Quantity = 2,
                        BaseQuantity = 2,
                        UnitPrice = 150000
                    }
                }
            };
            context.Orders.Add(coldOrder);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var dto = new DeliveryTripCreateDto
            {
                VehicleId = 1, // Xe máy thường, không có thùng lạnh
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 101 }
            };

            // Act & Assert
            var act = async () => await service.CreateTripAsync(dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*chuỗi lạnh*thùng lạnh*");
        }

        [Fact]
        public async Task TC04_CreateTrip_WhenOrderAlreadyAssignedOrCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var assignedOrder = new Order
            {
                Id = 102,
                OrderCode = "ORD-ASSIGNED-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Shipping,
                DeliveryTripId = 999
            };
            var completedOrder = new Order
            {
                Id = 103,
                OrderCode = "ORD-COMPLETED-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Completed
            };
            context.Orders.AddRange(assignedOrder, completedOrder);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);

            // Test 1: Đơn đã có chuyến
            var dto1 = new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 102 }
            };
            var act1 = async () => await service.CreateTripAsync(dto1);
            await act1.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã được gán vào chuyến xe khác*");

            // Test 2: Đơn đã hoàn tất
            var dto2 = new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 103 }
            };
            var act2 = async () => await service.CreateTripAsync(dto2);
            await act2.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*không thể điều phối vận chuyển*");
        }

        [Fact]
        public async Task TC05_StartTrip_ShouldUpdateStatusInTransit_AndSyncOrderDispatchedAt()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var order = new Order
            {
                Id = 104,
                OrderCode = "ORD-START-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var tripDto = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 104 }
            });

            // Act: Xuất bến
            var startedTrip = await service.StartTripAsync(tripDto.Id);

            // Assert
            startedTrip.Status.Should().Be("InTransit");
            startedTrip.StartedAt.Should().NotBeNull();

            var updatedOrder = await context.Orders.FindAsync(104);
            updatedOrder!.Status.Should().Be(OrderStatus.Shipping);
            updatedOrder.DispatchedAt.Should().Be(startedTrip.StartedAt);
        }

        [Fact]
        public async Task TC06_MarkTripOrderDelivered_CodOrder_ShouldReconcileCashAndSetPaid()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var codOrder = new Order
            {
                Id = 105,
                OrderCode = "ORD-COD-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Shipping,
                PaymentMethod = PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Unpaid,
                TotalAmount = 500000
            };
            context.Orders.Add(codOrder);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var tripDto = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 105 }
            });
            await service.StartTripAsync(tripDto.Id);

            // Act: Shipper giao thành công và thu tiền mặt
            await service.MarkTripOrderDeliveredAsync(tripDto.Id, 105, "Đã thu tiền COD đủ 500k");

            // Assert: Đơn hoàn tất và tiền mặt COD đã được xác nhận thanh toán (Paid)
            var updatedOrder = await context.Orders.FindAsync(105);
            updatedOrder!.Status.Should().Be(OrderStatus.Completed);
            updatedOrder.PaymentStatus.Should().Be(PaymentStatus.Paid);
            updatedOrder.DeliveredAt.Should().NotBeNull();
        }

        [Fact]
        public async Task TC07_CompleteTrip_WhenOrdersStillPending_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var order = new Order
            {
                Id = 106,
                OrderCode = "ORD-PENDING-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Shipping
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var tripDto = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 106 }
            });
            await service.StartTripAsync(tripDto.Id);

            // Act & Assert: Cố đóng chuyến khi chưa cập nhật kết quả giao đơn 106
            var act = async () => await service.CompleteTripAsync(tripDto.Id);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chờ giao (Pending)*");
        }

        [Fact]
        public async Task TC08_CompleteTrip_FailedOrder_ShouldAutoCreateDoorstepRefusalRma_FollowingFefoBatchRule()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            // Đơn có mặt hàng Dâu Tây (VariantId = 2).
            // Lô trong hệ thống: Batch 1 (hạn 30 ngày), Batch 2 (hạn 3 ngày).
            // Quy tắc FEFO bắt buộc chọn Batch 2 (cận hạn nhất) cho phiếu hoàn DoorstepRefusal.
            var order = new Order
            {
                Id = 107,
                OrderCode = "ORD-REFUSAL-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Shipping,
                Details = new List<OrderDetail>
                {
                    new OrderDetail
                    {
                        VariantId = 2,
                        UoMId = 1,
                        Quantity = 3,
                        BaseQuantity = 3,
                        UnitPrice = 120000
                    }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var tripDto = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 107 }
            });
            await service.StartTripAsync(tripDto.Id);

            // Shipper báo khách từ chối nhận
            await service.MarkTripOrderFailedAsync(tripDto.Id, 107, "Khách đi vắng không thể nhận hàng");

            // Act: Xe quay về kho và hoàn tất chuyến
            await service.CompleteTripAsync(tripDto.Id);

            // Assert
            var cancelledOrder = await context.Orders.FindAsync(107);
            cancelledOrder!.Status.Should().Be(OrderStatus.Cancelled);

            // Tự động sinh phiếu RMA DoorstepRefusal
            var rma = await context.CustomerReturns
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.OrderId == 107);

            rma.Should().NotBeNull();
            rma!.ReturnType.Should().Be(CustomerReturnType.DoorstepRefusal);
            rma.Status.Should().Be(CustomerReturnStatus.Inspecting);
            rma.Details.Should().HaveCount(1);

            // KIỂM TRA QUY TẮC FEFO: Lô hàng được chọn phải là Batch 2 (ExpiryDate cận ngày nhất: 3 ngày)
            var returnDetail = rma.Details.First();
            returnDetail.BatchId.Should().Be(2, "Quy tắc FEFO bắt buộc chọn lô có hạn sử dụng cận nhất (ExpiryDate sớm nhất)");
            returnDetail.ReturnedQuantity.Should().Be(3);
        }

        #endregion

        #region LUỒNG 2: THU HỒI ĐƠN TRẢ RMA (CUSTOMER RETURN PICKUP)

        [Fact]
        public async Task TC09_CreateTrip_ReturnPickup_WhenReturnNotApprovedOrAssigned_ShouldThrow()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var pendingRma = new CustomerReturn
            {
                Id = 201,
                ReturnCode = "RET-PENDING-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Pending // Chưa Approved
            };
            var assignedRma = new CustomerReturn
            {
                Id = 202,
                ReturnCode = "RET-ASSIGNED-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Approved,
                DeliveryTripId = 888 // Đã có chuyến khác
            };
            context.CustomerReturns.AddRange(pendingRma, assignedRma);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);

            // Test 1: Phiếu chưa duyệt
            var dto1 = new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Return",
                CustomerReturnIds = new List<int> { 201 }
            };
            var act1 = async () => await service.CreateTripAsync(dto1);
            await act1.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã duyệt (Approved)*");

            // Test 2: Phiếu đã gán chuyến khác
            var dto2 = new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Return",
                CustomerReturnIds = new List<int> { 202 }
            };
            var act2 = async () => await service.CreateTripAsync(dto2);
            await act2.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã được gán vào chuyến xe khác*");
        }

        [Fact]
        public async Task TC10_MarkTripReturnPickedUp_AndCompleteTrip_ShouldTransitionToInspecting()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var approvedRma = new CustomerReturn
            {
                Id = 203,
                ReturnCode = "RET-APPROVED-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Approved
            };
            context.CustomerReturns.Add(approvedRma);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var tripDto = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Return",
                CustomerReturnIds = new List<int> { 203 }
            });

            // Kiểm tra trạng thái chuyển sang PickingUp khi gán xe
            var rmaAfterAssign = await context.CustomerReturns.FindAsync(203);
            rmaAfterAssign!.Status.Should().Be(CustomerReturnStatus.PickingUp);

            // Act 1: Tài xế lấy hàng thành công
            await service.MarkTripReturnPickedUpAsync(tripDto.Id, 203, "Khách đóng gói nguyên tem niêm phong");

            // Act 2: Xe về tới kho và hoàn tất chuyến
            await service.CompleteTripAsync(tripDto.Id);

            // Assert: Hàng đã cập bến kho an toàn -> Chuyển sang Inspecting chờ QC kiểm định
            var finalRma = await context.CustomerReturns.FindAsync(203);
            finalRma!.Status.Should().Be(CustomerReturnStatus.Inspecting);
        }

        [Fact]
        public async Task TC11_MarkTripReturnFailed_ShouldResetToApproved_AndRemoveTripId()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var rma = new CustomerReturn
            {
                Id = 204,
                ReturnCode = "RET-FAIL-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Approved
            };
            context.CustomerReturns.Add(rma);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var tripDto = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Return",
                CustomerReturnIds = new List<int> { 204 }
            });

            // Act: Shipper tới nhà nhưng khách không nghe máy / hẹn lại
            await service.MarkTripReturnFailedAsync(tripDto.Id, 204, "Khách hẹn lại ngày mai");

            // Assert: Hoàn trả lại trạng thái Approved, gỡ DeliveryTripId để điều phối viên xếp chuyến sau
            var updatedRma = await context.CustomerReturns.FindAsync(204);
            updatedRma!.Status.Should().Be(CustomerReturnStatus.Approved);
            updatedRma.DeliveryTripId.Should().BeNull();
            updatedRma.InspectionNotes.Should().Contain("Khách hẹn lại ngày mai");
        }

        #endregion

        #region LUỒNG 3: CHUYỂN KHO B2B (WAREHOUSE TRANSFER TRANSPORT)

        [Fact]
        public async Task TC12_CreateTrip_B2BTransfer_RequiresColdTruck_AndDeductsStock_WithTransferOut()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            // Tạo phiếu chuyển kho trạng thái Draft
            var transfer = new InventoryTransfer
            {
                Id = 301,
                TransferCode = "TRF-20260920-001",
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Status = InventoryTransferStatus.Draft, // Chưa duyệt
                Details = new List<InventoryTransferDetail>
                {
                    new InventoryTransferDetail
                    {
                        VariantId = 2,
                        BatchId = 2,
                        UoMId = 1,
                        Quantity = 20 // Cần chuyển 20kg (Kho nguồn đang có 100kg)
                    }
                }
            };
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);

            // Test 0: Chặn gán xe khi phiếu chuyển kho chưa được duyệt (vẫn ở Draft)
            var dtoDraft = new DeliveryTripCreateDto
            {
                VehicleId = 3, // Xe tải lạnh
                WarehouseId = 1,
                TripType = "B2B_Transfer",
                InventoryTransferId = 301
            };
            var actDraft = async () => await service.CreateTripAsync(dtoDraft);
            await actDraft.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã được phê duyệt (Approved)*");

            // Quản lý phê duyệt lệnh chuyển kho (Approved)
            transfer.Status = InventoryTransferStatus.Approved;
            await context.SaveChangesAsync();

            // Test 1: Chặn nếu dùng xe máy hoặc xe không phải xe tải lạnh chuyên dụng
            var dtoBike = new DeliveryTripCreateDto
            {
                VehicleId = 1, // Xe máy
                WarehouseId = 1,
                TripType = "B2B_Transfer",
                InventoryTransferId = 301
            };
            var actBike = async () => await service.CreateTripAsync(dtoBike);
            await actBike.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Xe tải lạnh chuyên dụng (RefrigeratedTruck)*");

            // Test 2: Dùng đúng Xe tải lạnh (VehicleId = 3)
            var dtoTruck = new DeliveryTripCreateDto
            {
                VehicleId = 3,
                WarehouseId = 1,
                TripType = "B2B_Transfer",
                InventoryTransferId = 301
            };
            var tripResult = await service.CreateTripAsync(dtoTruck);

            // Assert
            tripResult.Status.Should().Be("InTransit");
            tripResult.StartedAt.Should().NotBeNull();

            // Phiếu chuyển kho chuyển sang InTransit và gán tài xế snapshot
            var updatedTransfer = await context.InventoryTransfers.FindAsync(301);
            updatedTransfer!.Status.Should().Be(InventoryTransferStatus.InTransit);
            updatedTransfer.DeliveryTripId.Should().Be(tripResult.Id);
            updatedTransfer.DriverName.Should().Be("Lê Văn Tải");

            // Tồn kho kho nguồn bị trừ 20kg: 100 - 20 = 80kg
            var sourceInv = await context.WarehouseInventories.FindAsync(1);
            sourceInv!.QuantityAvailable.Should().Be(80);

            // Sổ cái xuất kho TransferOut được tạo tự động
            var txn = await context.InventoryTransactions
                .FirstOrDefaultAsync(t => t.WarehouseId == 1 && t.VariantId == 2 && t.BatchId == 2);
            txn.Should().NotBeNull();
            txn!.Type.Should().Be(TransactionType.TransferOut);
            txn.Quantity.Should().Be(20);
            txn.ReferenceCode.Should().Be("TRF-20260920-001");
        }

        [Fact]
        public async Task TC13_CompleteTrip_B2BTransfer_WhenDestinationNotCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var transfer = new InventoryTransfer
            {
                Id = 302,
                TransferCode = "TRF-20260920-002",
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Status = InventoryTransferStatus.Approved, // Đã duyệt
                Details = new List<InventoryTransferDetail>
                {
                    new InventoryTransferDetail
                    {
                        VariantId = 2,
                        BatchId = 2,
                        UoMId = 1,
                        Quantity = 10
                    }
                }
            };
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var trip = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 3,
                WarehouseId = 1,
                TripType = "B2B_Transfer",
                InventoryTransferId = 302
            });

            // Act & Assert: Kho đích chưa hoàn tất nghiệm thu (transfer vẫn InTransit), TMS cố đóng chuyến xe -> Bị chặn
            var act = async () => await service.CompleteTripAsync(trip.Id);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho đích đã nghiệm thu nhận hàng*");

            // Giả lập Kho đích đã nghiệm thu nhập kho hoàn tất (Completed)
            var transferInDb = await context.InventoryTransfers.FindAsync(302);
            transferInDb!.Status = InventoryTransferStatus.Completed;
            await context.SaveChangesAsync();

            // Giờ đóng chuyến xe sẽ thành công
            var completedTrip = await service.CompleteTripAsync(trip.Id);
            completedTrip.Status.Should().Be("Completed");

            // Xe tải được giải phóng về Available
            var truck = await context.DeliveryVehicles.FindAsync(3);
            truck!.Status.Should().Be("Available");
        }

        #endregion

        #region HỦY CHUYẾN XE (CANCEL TRIP)

        [Fact]
        public async Task TC14_CancelTrip_ShouldReleaseVehicle_RevertOrdersAndReturns_AndClearDriverSnapshot()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var order = new Order
            {
                Id = 108,
                OrderCode = "ORD-CANCEL-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed
            };
            var rma = new CustomerReturn
            {
                Id = 205,
                ReturnCode = "RET-CANCEL-001",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Approved
            };
            context.Orders.Add(order);
            context.CustomerReturns.Add(rma);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);

            // Tạo chuyến B2C Delivery
            var trip = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 108 }
            });

            // Gán thêm phiếu RMA vào chuyến
            var rmaInDb = await context.CustomerReturns.FindAsync(205);
            rmaInDb!.DeliveryTripId = trip.Id;
            rmaInDb.Status = CustomerReturnStatus.PickingUp;
            await context.SaveChangesAsync();

            // Act: Hủy chuyến xe vì sự cố thời tiết / xe hỏng
            var cancelledTrip = await service.CancelTripAsync(trip.Id, "Xe gặp sự cố thủng lốp, hoãn chuyến");

            // Assert
            cancelledTrip.Status.Should().Be("Cancelled");

            // Xe máy giải phóng về Available
            var vehicle = await context.DeliveryVehicles.FindAsync(2);
            vehicle!.Status.Should().Be("Available");

            // Đơn hàng B2C hoàn trả về Confirmed, xóa sạch snapshot tài xế
            var revertedOrder = await context.Orders.FindAsync(108);
            revertedOrder!.Status.Should().Be(OrderStatus.Confirmed);
            revertedOrder.DeliveryTripId.Should().BeNull();
            revertedOrder.DriverName.Should().BeNull();
            revertedOrder.DriverPhone.Should().BeNull();
            revertedOrder.LicensePlate.Should().BeNull();

            // Phiếu trả RMA hoàn trả về Approved để điều phối lại
            var revertedRma = await context.CustomerReturns.FindAsync(205);
            revertedRma!.Status.Should().Be(CustomerReturnStatus.Approved);
            revertedRma.DeliveryTripId.Should().BeNull();
            revertedRma.InspectionNotes.Should().Contain("bị hủy");
        }

        [Fact]
        public async Task TC15_CancelTrip_WithoutReason_ShouldThrowArgumentException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedBasicDataAsync(context);

            var order = new Order
            {
                Id = 109,
                OrderCode = "ORD-CANCEL-002",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Confirmed
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new DeliveryTripService(context, _mapper);
            var trip = await service.CreateTripAsync(new DeliveryTripCreateDto
            {
                VehicleId = 2,
                WarehouseId = 1,
                TripType = "B2C_Delivery",
                OrderIds = new List<int> { 109 }
            });

            // Act & Assert
            var act = async () => await service.CancelTripAsync(trip.Id, "   ");
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Lý do hủy chuyến xe không được để trống*");
        }

        #endregion
    }
}
