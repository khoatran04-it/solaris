using AutoMapper;
using backend.DTOs.InventoryReceiptDTOs;
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
    /// 📥 MODULE 10: INVENTORY RECEIPTS & QUALITY CONTROL (GRN)
    /// 🧪 TEST SUITE: InventoryReceiptServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Quản lý Phiếu Nhập Kho và Kiểm đếm nông sản:
    /// - Phân trang, tìm kiếm mã phiếu, lọc theo Kho, Nhà cung cấp, Trạng thái, Khoảng ngày
    /// - Lấy chi tiết phiếu nhập kèm danh sách dòng hàng (Details) đã làm phẳng AutoMapper
    /// - Tạo mới phiếu nhập ở trạng thái Pending (Chưa làm thay đổi số dư tồn kho)
    /// - Validation dữ liệu khi tạo: bắt buộc có dòng chi tiết, kiểm tra kho và NCC tồn tại
    /// - Hoàn tất nhập kho (CompleteReceipt): Chốt sổ, TĂNG tồn khả dụng, GHI sổ cái, ĐỒNG BỘ tiến độ nhận đơn PO
    /// - Khiên an toàn (Safety Shields): Chặn chốt sổ khi phiếu không ở trạng thái Pending/Inspecting
    /// - Hủy phiếu nhập kho (CancelReceipt) với lý do hủy bắt buộc
    /// - Khiên bảo vệ Sổ cái: Nghiêm cấm Hủy hoặc Xóa (Soft Delete) phiếu đã chốt sổ (Completed)
    /// </summary>
    public class InventoryReceiptServiceTests
    {
        private readonly IMapper _mapper;

        public InventoryReceiptServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Seed
        private static async Task SeedDependenciesAsync(Data.SolarisDbContext context)
        {
            // Seed User
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                CitizenId = "001200001234",
                Username = "inspector",
                FullName = "Nguyễn Kiểm Đếm",
                Email = "qc@solaris.vn",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                IsActive = true
            });

            // Seed Warehouse
            context.Warehouses.Add(new Warehouse
            {
                Id = 1,
                Code = "WH-HN-01",
                Name = "Tổng Kho Hà Nội",
                IsActive = true
            });

            // Seed Supplier
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "SUP-DALAT",
                Name = "Nông Trại Đà Lạt GAP",
                Phone = "0901234567",
                Email = "supplier@solaris.vn",
                IsActive = true
            });

            // Seed UoM
            context.UoMs.Add(new UoM
            {
                Id = 1,
                Code = "BOX",
                Name = "Hộp 500g",
                IsActive = true
            });

            // Seed Product & Variant
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
                IsActive = true
            });

            // Seed Batch
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-2026-001",
                VariantId = 1,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-2),
                ExpiryDate = DateTime.UtcNow.AddDays(15),
                IsActive = true
            });

            // Seed Purchase Order with detail
            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-20260830-001",
                SupplierId = 1,
                Status = PurchaseOrderStatus.Approved,
                TotalAmount = 5000000,
                CreatedById = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            po.Details.Add(new PurchaseOrderDetail
            {
                Id = 1,
                PurchaseOrderId = 1,
                VariantId = 1,
                UoMId = 1,
                OrderQuantity = 100,
                ReceivedQuantity = 0,
                UnitPrice = 50000,
                TotalPrice = 5000000
            });
            context.PurchaseOrders.Add(po);

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: GET PAGED ASYNC
        [Fact]
        public async Task TC01_GetPagedAsync_ReturnsPagedReceipts_WithFilters()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryReceipts.AddRange(
                new InventoryReceipt
                {
                    Id = 1,
                    ReceiptCode = "IR-20260830-000001",
                    WarehouseId = 1,
                    SupplierId = 1,
                    Status = InventoryReceiptStatus.Pending,
                    Note = "Nhập hàng đợt 1",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new InventoryReceipt
                {
                    Id = 2,
                    ReceiptCode = "IR-20260830-000002",
                    WarehouseId = 1,
                    SupplierId = 1,
                    Status = InventoryReceiptStatus.Completed,
                    Note = "Đã chốt sổ xong",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act 1: Lọc theo status Pending
            var pendingResult = await service.GetPagedAsync(null, null, null, (int)InventoryReceiptStatus.Pending, null, null, 1, 10);
            pendingResult.TotalRecords.Should().Be(1);
            pendingResult.Items.First().ReceiptCode.Should().Be("IR-20260830-000001");

            // Act 2: Tìm kiếm theo mã phiếu
            var searchResult = await service.GetPagedAsync("000002", null, null, null, null, null, 1, 10);
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().ReceiptCode.Should().Be("IR-20260830-000002");
        }
        #endregion

        #region TC02: GET BY ID ASYNC
        [Fact]
        public async Task TC02_GetByIdAsync_ReturnsReceiptWithFlattenedDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var receipt = new InventoryReceipt
            {
                Id = 10,
                ReceiptCode = "IR-20260830-TEST01",
                WarehouseId = 1,
                SupplierId = 1,
                ReceivedById = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 10,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                PurchaseOrderDetailId = 1,
                ExpectedQuantity = 50,
                AcceptedQuantity = 48,
                RejectedQuantity = 2,
                RejectReason = "Dập vỏ bao bì"
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act & Assert 1: Lấy thành công
            var result = await service.GetByIdAsync(10);
            result.Should().NotBeNull();
            result.ReceiptCode.Should().Be("IR-20260830-TEST01");
            result.WarehouseName.Should().Be("Tổng Kho Hà Nội");
            result.SupplierName.Should().Be("Nông Trại Đà Lạt GAP");
            result.ReceivedByName.Should().Be("Nguyễn Kiểm Đếm");

            result.Details.Should().HaveCount(1);
            var detailDto = result.Details.First();
            detailDto.VariantName.Should().Be("Dâu Tây Hộp 500g");
            detailDto.BatchCode.Should().Be("BATCH-2026-001");
            detailDto.UoMName.Should().Be("Hộp 500g");
            detailDto.AcceptedQuantity.Should().Be(48);
            detailDto.RejectedQuantity.Should().Be(2);

            // Act & Assert 2: ID không tồn tại
            var notFoundAct = async () => await service.GetByIdAsync(999);
            await notFoundAct.Should().ThrowAsync<KeyNotFoundException>();
        }
        #endregion

        #region TC03: CREATE ASYNC
        [Fact]
        public async Task TC03_CreateAsync_CreatesReceiptInPendingStatus_WithUniqueReceiptCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryReceiptService(context, _mapper);

            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Note = "Nhập hàng từ xe tải 29H-123.45",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        PurchaseOrderDetailId = 1,
                        ExpectedQuantity = 100,
                        AcceptedQuantity = 95,
                        RejectedQuantity = 5,
                        RejectReason = "Héo cuống"
                    }
                }
            };

            // Act
            var newId = await service.CreateAsync(createDto);

            // Assert
            var entity = await context.InventoryReceipts.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == newId);
            entity.Should().NotBeNull();
            entity!.Status.Should().Be(InventoryReceiptStatus.Pending);
            entity.ReceiptCode.Should().StartWith("IR-");
            entity.Details.Should().HaveCount(1);
            entity.Details.First().AcceptedQuantity.Should().Be(95);

            // Đảm bảo tạo phiếu CHƯA cộng vào tồn kho
            var inv = await context.WarehouseInventories.FirstOrDefaultAsync(x => x.WarehouseId == 1 && x.VariantId == 1);
            inv.Should().BeNull();
        }
        #endregion

        #region TC04: CREATE ASYNC VALIDATION
        [Fact]
        public async Task TC04_CreateAsync_ValidationShields_ThrowsException_WhenInvalidData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryReceiptService(context, _mapper);

            // Act & Assert 1: Danh sách details rỗng
            var emptyDetailsDto = new InventoryReceiptCreateDto { WarehouseId = 1, Details = new List<InventoryReceiptDetailCreateDto>() };
            var act1 = async () => await service.CreateAsync(emptyDetailsDto);
            await act1.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ít nhất 1 dòng*");

            // Act & Assert 2: Kho không tồn tại
            var invalidWhDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 999,
                Details = new List<InventoryReceiptDetailCreateDto> { new() { VariantId = 1, BatchId = 1, UoMId = 1 } }
            };
            var act2 = async () => await service.CreateAsync(invalidWhDto);
            await act2.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho nhận hàng*không tồn tại*");
        }
        #endregion

        #region TC05: COMPLETE RECEIPT ASYNC
        [Fact]
        public async Task TC05_CompleteReceiptAsync_IncreasesStock_LogsLedger_AndUpdatesPOStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var receipt = new InventoryReceipt
            {
                Id = 1,
                ReceiptCode = "IR-20260830-COMPLETE",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 1,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                PurchaseOrderDetailId = 1,
                ExpectedQuantity = 100,
                AcceptedQuantity = 100, // Nhận đủ 100/100 của PO
                RejectedQuantity = 0
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act: Hoàn tất phiếu nhập kho
            var success = await service.CompleteReceiptAsync(1, receivedById: 1, note: "Kiểm đếm đạt chuẩn 100%");

            // Assert
            success.Should().BeTrue();

            // 1. Trạng thái phiếu nhập chuyển Completed
            var completedReceipt = await context.InventoryReceipts.FindAsync(1);
            completedReceipt!.Status.Should().Be(InventoryReceiptStatus.Completed);
            completedReceipt.ReceiptDate.Should().NotBeNull();
            completedReceipt.Note.Should().Be("Kiểm đếm đạt chuẩn 100%");

            // 2. Tồn kho khả dụng tăng 100
            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityAvailable.Should().Be(100);

            // 3. Sổ cái được ghi nhận
            var txn = await context.InventoryTransactions.FirstAsync(x => x.ReferenceCode == "IR-20260830-COMPLETE");
            txn.Type.Should().Be(TransactionType.Receipt);
            txn.Quantity.Should().Be(100);

            // 4. Đơn PO và PO Detail đồng bộ số lượng và chuyển Completed
            var poDetail = await context.PurchaseOrderDetails.FindAsync(1);
            poDetail!.ReceivedQuantity.Should().Be(100);

            var po = await context.PurchaseOrders.FindAsync(1);
            po!.Status.Should().Be(PurchaseOrderStatus.Completed);
        }
        #endregion

        #region TC06: COMPLETE RECEIPT SAFETY SHIELD
        [Fact]
        public async Task TC06_CompleteReceiptAsync_SafetyShields_ThrowsException_WhenInvalidState()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryReceipts.Add(new InventoryReceipt
            {
                Id = 2,
                ReceiptCode = "IR-ALREADY-COMPLETED",
                WarehouseId = 1,
                Status = InventoryReceiptStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act & Assert: Chốt sổ lại phiếu đã hoàn tất -> Ném ngoại lệ chặn
            var act = async () => await service.CompleteReceiptAsync(2, 1, null);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phải ở trạng thái Chờ nhập kho*");
        }
        #endregion

        #region TC07: CANCEL RECEIPT ASYNC
        [Fact]
        public async Task TC07_CancelReceiptAsync_CancelsPendingReceipt_WithMandatoryReason()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryReceipts.Add(new InventoryReceipt
            {
                Id = 3,
                ReceiptCode = "IR-PENDING-CANCEL",
                WarehouseId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act 1: Bỏ trống lý do hủy -> Báo lỗi
            var emptyReasonAct = async () => await service.CancelReceiptAsync(3, "");
            await emptyReasonAct.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Lý do hủy*không được để trống*");

            // Act 2: Hủy với lý do hợp lệ
            var success = await service.CancelReceiptAsync(3, "NCC giao sai loại quả");
            success.Should().BeTrue();

            var cancelledReceipt = await context.InventoryReceipts.FindAsync(3);
            cancelledReceipt!.Status.Should().Be(InventoryReceiptStatus.Cancelled);
            cancelledReceipt.CancellationReason.Should().Be("NCC giao sai loại quả");
        }
        #endregion

        #region TC08: CANCEL & DELETE SAFETY SHIELDS FOR COMPLETED RECEIPTS
        [Fact]
        public async Task TC08_CancelAndSoftDelete_SafetyShields_BlocksWhenReceiptAlreadyCompleted()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryReceipts.Add(new InventoryReceipt
            {
                Id = 4,
                ReceiptCode = "IR-PROTECTED-COMPLETED",
                WarehouseId = 1,
                Status = InventoryReceiptStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act & Assert 1: Chặn hủy phiếu đã Completed
            var cancelAct = async () => await service.CancelReceiptAsync(4, "Muốn hủy");
            await cancelAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể hủy Phiếu Nhập Kho đã hoàn tất*");

            // Act & Assert 2: Chặn xóa phiếu đã Completed
            var deleteAct = async () => await service.DeleteAsync(4);
            await deleteAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa Phiếu Nhập Kho đã hoàn tất*");
        }
        #endregion
    }
}
