using AutoMapper;
using backend.DTOs.InventoryTransferDTOs;
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

namespace backend.Tests.Modules.Module10_Inventory
{
    /// <summary>
    /// ============================================================================
    /// MODULE 10: INVENTORY TRANSFERS (2-STEP DISPATCH & RECEIVE)
    /// TEST SUITE: InventoryTransferServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Quản lý Phiếu Điều Chuyển Hàng Liên Kho:
    /// - Phân trang, tìm kiếm mã phiếu, lọc theo Kho Nguồn, Kho Đích, Trạng thái, Khoảng ngày
    /// - Phân quyền RBAC 2 chiều: User được xem phiếu nếu Kho Nguồn HOẶC Kho Đích nằm trong quyền
    /// - Lấy chi tiết phiếu chuyển kèm danh sách mặt hàng (Details) làm phẳng AutoMapper
    /// - Tạo mới phiếu chuyển ở trạng thái Draft, tự sinh mã TRF-yyyyMMdd-XXXXXX
    /// - Bắt lỗi validation khi tạo: trùng kho nguồn - đích, danh sách hàng rỗng, kho không tồn tại
    /// - BƯỚC 1 (Dispatch): Trừ tồn kho tại Kho Nguồn, Ghi sổ cái TransferOut, chuyển trạng thái InTransit
    /// - BƯỚC 2 (Receive): Cộng tồn kho tại Kho Đích (bảo toàn BatchId), Ghi sổ cái TransferIn, chuyển Completed
    /// - Hủy phiếu điều chuyển (CancelTransfer) với lý do hủy bắt buộc (Chỉ cho phép khi ở trạng thái Draft)
    /// - Khiên an toàn: Chặn xuất hàng khi kho nguồn không đủ số lượng, chặn nhận hàng khi chưa InTransit
    /// </summary>
    public class InventoryTransferServiceTests
    {
        private readonly IMapper _mapper;

        public InventoryTransferServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Seed
        private static async Task SeedDependenciesAsync(Data.SolarisDbContext context)
        {
            // Seed User
            context.IAUsers.AddRange(
                new IAUser { Id = 1, Username = "admin", FullName = "Nguyễn Quản Trị", Email = "admin@solaris.vn", PhoneNumber = "0901", PasswordHash = "h", CitizenId = "001", IsActive = true },
                new IAUser { Id = 2, Username = "dispatch_mgr", FullName = "Trần Xuất Kho", Email = "out@solaris.vn", PhoneNumber = "0902", PasswordHash = "h", CitizenId = "002", IsActive = true },
                new IAUser { Id = 3, Username = "receive_mgr", FullName = "Lê Nhận Kho", Email = "in@solaris.vn", PhoneNumber = "0903", PasswordHash = "h", CitizenId = "003", IsActive = true }
            );

            // Seed Warehouses
            context.Warehouses.AddRange(
                new Warehouse { Id = 1, Code = "WH-HN-01", Name = "Tổng Kho Hà Nội", IsActive = true },
                new Warehouse { Id = 2, Code = "WH-HCM-01", Name = "Kho Nam Sài Gòn", IsActive = true }
            );

            // Seed Supplier & UoM
            context.Suppliers.Add(new Supplier { Id = 1, Code = "SUP-DALAT", Name = "Nông Trại Đà Lạt GAP", Phone = "0901234567", Email = "supplier@solaris.vn", IsActive = true });
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true });

            // Seed Product & Variant
            context.Products.Add(new Product { Id = 1, Code = "PROD-DAUTAY", Name = "Dâu Tây Đà Lạt", CategoryId = 1, BaseUoMId = 1, IsActive = true });
            context.ProductVariants.Add(new ProductVariant { Id = 1, ProductId = 1, Code = "SKU-DAUTAY-500G", Name = "Dâu Tây Hộp 500g", IsActive = true });

            // Seed Batch
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-TRANSFER-001",
                VariantId = 1,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-5),
                ExpiryDate = DateTime.UtcNow.AddDays(25),
                IsActive = true
            });

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: GET PAGED ASYNC
        [Fact]
        public async Task TC01_GetPagedAsync_ReturnsPagedTransfers_WithFilters_AndRBAC()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryTransfers.AddRange(
                new InventoryTransfer
                {
                    Id = 1,
                    TransferCode = "TRF-20260830-000001",
                    FromWarehouseId = 1,
                    ToWarehouseId = 2,
                    Status = InventoryTransferStatus.Draft,
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new InventoryTransfer
                {
                    Id = 2,
                    TransferCode = "TRF-20260830-000002",
                    FromWarehouseId = 2,
                    ToWarehouseId = 1,
                    Status = InventoryTransferStatus.InTransit,
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            // Act 1: Lấy toàn cục
            var allResult = await service.GetPagedAsync(null, null, null, null, null, null, 1, 10);
            allResult.TotalRecords.Should().Be(2);

            // Act 2: Lọc theo Kho nguồn (FromWarehouseId = 1)
            var fromWhResult = await service.GetPagedAsync(null, fromWarehouseId: 1, null, null, null, null, 1, 10);
            fromWhResult.TotalRecords.Should().Be(1);
            fromWhResult.Items.First().TransferCode.Should().Be("TRF-20260830-000001");

            // Act 3: Lọc phân quyền RBAC (User chỉ được gán Kho 2 -> Vẫn xem được cả 2 vì Kho 2 là Đích của phiếu 1 và Nguồn của phiếu 2)
            var rbacResult = await service.GetPagedAsync(null, null, null, null, null, null, 1, 10, new List<int> { 2 });
            rbacResult.TotalRecords.Should().Be(2);
        }
        #endregion

        #region TC02: GET BY ID ASYNC
        [Fact]
        public async Task TC02_GetByIdAsync_ReturnsTransfer_WithFlattenedDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var transfer = new InventoryTransfer
            {
                Id = 10,
                TransferCode = "TRF-20260830-TEST01",
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                CreatedById = 1,
                DispatchedById = 2,
                ReceivedById = 3,
                Status = InventoryTransferStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            transfer.Details.Add(new InventoryTransferDetail
            {
                Id = 1,
                InventoryTransferId = 10,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                Quantity = 60
            });
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            // Act & Assert 1: Lấy thành công
            var result = await service.GetByIdAsync(10, new List<int> { 1 });
            result.Should().NotBeNull();
            result.TransferCode.Should().Be("TRF-20260830-TEST01");
            result.FromWarehouseName.Should().Be("Tổng Kho Hà Nội");
            result.ToWarehouseName.Should().Be("Kho Nam Sài Gòn");
            result.CreatedByName.Should().Be("Nguyễn Quản Trị");
            result.DispatchedByName.Should().Be("Trần Xuất Kho");
            result.ReceivedByName.Should().Be("Lê Nhận Kho");

            result.Details.Should().HaveCount(1);
            var detailDto = result.Details.First();
            detailDto.VariantName.Should().Be("Dâu Tây Hộp 500g");
            detailDto.BatchCode.Should().Be("BATCH-TRANSFER-001");
            detailDto.Quantity.Should().Be(60);

            // Act & Assert 2: Không tìm thấy ID
            var notFoundAct = async () => await service.GetByIdAsync(999);
            await notFoundAct.Should().ThrowAsync<KeyNotFoundException>();
        }
        #endregion

        #region TC03: CREATE ASYNC
        [Fact]
        public async Task TC03_CreateAsync_CreatesTransferInDraftStatus_WithUniqueTransferCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Setup tồn kho khả dụng tại Kho nguồn (100 > 40)
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            var createDto = new InventoryTransferCreateDto
            {
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Note = "Điều chuyển cân đối tồn kho Bắc Nam",
                Details = new List<InventoryTransferDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        Quantity = 40
                    }
                }
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            var entity = await context.InventoryTransfers.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == newId);
            entity.Should().NotBeNull();
            entity!.Status.Should().Be(InventoryTransferStatus.Draft);
            entity.TransferCode.Should().StartWith("TRF-");
            entity.Details.Should().HaveCount(1);
            entity.Details.First().Quantity.Should().Be(40);
        }
        #endregion

        #region TC04: CREATE ASYNC VALIDATION
        [Fact]
        public async Task TC04_CreateAsync_ValidationShields_ThrowsException_WhenInvalidData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryTransferService(context, _mapper);

            // Act & Assert 1: Trùng kho nguồn và đích
            var sameWhDto = new InventoryTransferCreateDto
            {
                FromWarehouseId = 1,
                ToWarehouseId = 1,
                Details = new List<InventoryTransferDetailCreateDto> { new() { VariantId = 1, BatchId = 1, UoMId = 1, Quantity = 10 } }
            };
            var act1 = async () => await service.CreateAsync(sameWhDto);
            await act1.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*không được trùng nhau*");

            // Act & Assert 2: Danh sách details rỗng
            var emptyDetailsDto = new InventoryTransferCreateDto { FromWarehouseId = 1, ToWarehouseId = 2, Details = new List<InventoryTransferDetailCreateDto>() };
            var act2 = async () => await service.CreateAsync(emptyDetailsDto);
            await act2.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ít nhất 1 dòng chi tiết*");
        }
        #endregion

        #region TC05: DISPATCH TRANSFER ASYNC (STEP 1)
        [Fact]
        public async Task TC05_DispatchTransferAsync_DeductsSourceStock_LogsTransferOut_AndTransitionsToInTransit()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Setup tồn kho nguồn tại Kho 1 (QuantityAvailable = 100)
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var transfer = new InventoryTransfer
            {
                Id = 1,
                TransferCode = "TRF-20260830-DISPATCH",
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Status = InventoryTransferStatus.Draft,
                CreatedById = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            transfer.Details.Add(new InventoryTransferDetail
            {
                Id = 1,
                InventoryTransferId = 1,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                Quantity = 40
            });
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            // Bước 1: Quản lý phê duyệt lệnh chuyển kho (Approved)
            var approveSuccess = await service.ApproveTransferAsync(1, approvedById: 1, "Duyệt xuất hàng sang chi nhánh 2");
            approveSuccess.Should().BeTrue();

            var approvedTransfer = await context.InventoryTransfers.FindAsync(1);
            approvedTransfer!.Status.Should().Be(InventoryTransferStatus.Approved);
            approvedTransfer.ApprovedById.Should().Be(1);
            approvedTransfer.ApprovedDate.Should().NotBeNull();
            approvedTransfer.ApprovalNote.Should().Be("Duyệt xuất hàng sang chi nhánh 2");

            // Bước 2: Xuất phát chuyến hàng (Dispatched)
            var success = await service.DispatchTransferAsync(1, dispatchedById: 2);

            // Assert
            success.Should().BeTrue();

            // 1. Phiếu chuyển trạng thái InTransit
            var dispatchedTransfer = await context.InventoryTransfers.FindAsync(1);
            dispatchedTransfer!.Status.Should().Be(InventoryTransferStatus.InTransit);
            dispatchedTransfer.DispatchedById.Should().Be(2);
            dispatchedTransfer.DispatchedDate.Should().NotBeNull();

            // 2. Tồn kho kho nguồn bị trừ 40 (100 -> 60)
            var sourceInv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            sourceInv.QuantityAvailable.Should().Be(60);

            // 3. Sổ cái ghi nhận TransferOut
            var txn = await context.InventoryTransactions.FirstAsync(x => x.ReferenceCode == "TRF-20260830-DISPATCH");
            txn.Type.Should().Be(TransactionType.TransferOut);
            txn.Quantity.Should().Be(40);
        }
        #endregion

        #region TC06: DISPATCH TRANSFER SAFETY SHIELDS
        [Fact]
        public async Task TC06_DispatchTransferAsync_SafetyShields_ThrowsException_WhenInsufficientStockOrInvalidState()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Kho 1 chỉ có 10kg
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var transfer = new InventoryTransfer
            {
                Id = 2,
                TransferCode = "TRF-20260830-OVER",
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Status = InventoryTransferStatus.Draft,
                CreatedById = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            transfer.Details.Add(new InventoryTransferDetail
            {
                InventoryTransferId = 2,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                Quantity = 50 // Đòi xuất 50kg trong khi chỉ có 10kg
            });
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            // Test 1: Chặn xuất kho khi phiếu chưa được duyệt (vẫn ở Draft)
            var actDraft = async () => await service.DispatchTransferAsync(2, 2);
            await actDraft.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phê duyệt (Approved)*");

            // Test 2: Chặn phê duyệt khi kho nguồn không đủ số lượng tồn kho khả dụng
            var actApprove = async () => await service.ApproveTransferAsync(2, 1);
            await actApprove.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho nguồn không đủ số lượng khả dụng*");
        }
        #endregion

        #region TC07: RECEIVE TRANSFER ASYNC (STEP 2)
        [Fact]
        public async Task TC07_ReceiveTransferAsync_AddsTargetStock_LogsTransferIn_AndTransitionsToCompleted()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var transfer = new InventoryTransfer
            {
                Id = 3,
                TransferCode = "TRF-20260830-RECEIVE",
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Status = InventoryTransferStatus.InTransit,
                DispatchedById = 2,
                DispatchedDate = DateTime.UtcNow.AddDays(-1),
                CreatedById = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            transfer.Details.Add(new InventoryTransferDetail
            {
                Id = 1,
                InventoryTransferId = 3,
                VariantId = 1,
                BatchId = 1, // Bảo toàn BatchId gốc
                UoMId = 1,
                Quantity = 40
            });
            context.InventoryTransfers.Add(transfer);
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            // Act: Nhận hàng tại Kho đích (Bước 2)
            var success = await service.ReceiveTransferAsync(3, receivedById: 3);

            // Assert
            success.Should().BeTrue();

            // 1. Phiếu chuyển trạng thái Completed
            var completedTransfer = await context.InventoryTransfers.FindAsync(3);
            completedTransfer!.Status.Should().Be(InventoryTransferStatus.Completed);
            completedTransfer.ReceivedById.Should().Be(3);
            completedTransfer.ReceivedDate.Should().NotBeNull();

            // 2. Tồn kho kho đích (Kho 2) được tạo mới/cộng 40kg với đúng BatchId gốc
            var targetInv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 2 && x.VariantId == 1 && x.BatchId == 1);
            targetInv.QuantityAvailable.Should().Be(40);

            // 3. Sổ cái ghi nhận TransferIn
            var txn = await context.InventoryTransactions.FirstAsync(x => x.ReferenceCode == "TRF-20260830-RECEIVE");
            txn.Type.Should().Be(TransactionType.TransferIn);
            txn.Quantity.Should().Be(40);
        }
        #endregion

        #region TC08: CANCEL TRANSFER ASYNC & SHIELDS
        [Fact]
        public async Task TC08_CancelTransferAsync_CancelsDraftTransfer_WithMandatoryReason_AndBlocksInTransit()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryTransfers.AddRange(
                new InventoryTransfer
                {
                    Id = 4,
                    TransferCode = "TRF-DRAFT-CANCEL",
                    FromWarehouseId = 1,
                    ToWarehouseId = 2,
                    Status = InventoryTransferStatus.Draft,
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new InventoryTransfer
                {
                    Id = 5,
                    TransferCode = "TRF-INTRANSIT-CANCEL",
                    FromWarehouseId = 1,
                    ToWarehouseId = 2,
                    Status = InventoryTransferStatus.InTransit,
                    CreatedById = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            // Act 1: Hủy phiếu Draft thành công với lý do
            var success = await service.CancelTransferAsync(4, "Đổi kế hoạch kinh doanh");
            success.Should().BeTrue();

            var cancelled = await context.InventoryTransfers.FindAsync(4);
            cancelled!.Status.Should().Be(InventoryTransferStatus.Cancelled);
            cancelled.CancellationReason.Should().Be("Đổi kế hoạch kinh doanh");

            // Act 2 & Assert 2: Chặn hủy phiếu khi hàng đã lên xe (InTransit)
            var inTransitCancelAct = async () => await service.CancelTransferAsync(5, "Muốn hủy");
            await inTransitCancelAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ được phép hủy khi phiếu chuyển kho đang ở trạng thái Nháp*");
        }
        #endregion

        #region TC09: SCM SHIELD - CHẶN ĐIỀU CHUYỂN XUẤT PHÁT TỪ KHO HÀNG LỖI
        /// <summary>
        /// TC09: SCM Rule - Cấm xuất phát từ Kho Hàng Lỗi (Damaged). Hàng đã vào kho lỗi/hủy là trạm cuối.
        /// </summary>
        [Fact]
        public async Task CreateAsync_FromDamagedWarehouse_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var damagedWh = new Warehouse
            {
                Id = 10,
                Code = "WH-DAMAGED",
                Name = "Kho Hàng Hỏng & Hủy",
                WarehouseType = WarehouseTypeConstants.Damaged,
                IsActive = true
            };
            context.Warehouses.Add(damagedWh);
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            var dto = new InventoryTransferCreateDto
            {
                FromWarehouseId = 10,
                ToWarehouseId = 2,
                CreatedById = 1,
                Details = new List<InventoryTransferDetailCreateDto>
                {
                    new InventoryTransferDetailCreateDto { VariantId = 1, BatchId = 1, UoMId = 1, Quantity = 10 }
                }
            };

            // Act & Assert
            var act = () => service.CreateAsync(dto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho Hàng Lỗi không được phép điều chuyển*");
        }
        #endregion

        #region TC10: CREATE ASYNC - CHẶN CHUYỂN QUÁ SỐ LƯỢNG TỒN KHO KHẢ DỤNG
        [Fact]
        public async Task TC10_CreateAsync_WhenQuantityExceedsAvailableStock_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Kho 1 chỉ có 20kg tồn khả dụng
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryTransferService(context, _mapper);

            var createDto = new InventoryTransferCreateDto
            {
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                Note = "Điều chuyển vượt tồn",
                Details = new List<InventoryTransferDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        Quantity = 50 // 50 > 20
                    }
                }
            };

            // Act & Assert
            var act = () => service.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho nguồn không đủ số lượng tồn kho khả dụng*");
        }
        #endregion
    }
}
