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
    /// MODULE 10: INVENTORY RECEIPTS & QUALITY CONTROL (GRN)
    /// TEST SUITE: InventoryReceiptServiceTests
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

        #region TC09: PHÂN LOẠI HÀNG TỪ CHỐI TẠI DOCK VS HÀNG HỎNG RMA THU HỒI
        /// <summary>
        /// TC09: Kiểm tra khi hoàn tất phiếu nhập từ Nhà cung cấp có RejectedQuantity > 0,
        /// hàng bị từ chối giữ nguyên trên xe NCC (không nhập kho), KHÔNG cộng vào QuantityDamaged.
        /// </summary>
        [Fact]
        public async Task TC09_CompleteReceiptAsync_WithRejectedQuantity_FromSupplier_ShouldNotRouteToQuantityDamaged()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var receipt = new InventoryReceipt
            {
                Id = 15,
                ReceiptCode = "IR-SUPPLIER-REJECT-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 15,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                ExpectedQuantity = 100,
                AcceptedQuantity = 90,
                RejectedQuantity = 10,
                RejectReason = "10 hộp bị dập nát, trả lại xe NCC ngay tại dock"
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(15, receivedById: 1, note: "Nhập hàng thực nhận 90, trả lại NCC 10");

            // Assert
            success.Should().BeTrue();

            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityAvailable.Should().Be(90);  // Chỉ hàng thực nhận nhập kho
            inv.QuantityDamaged.Should().Be(0);    // Hàng NCC bị trả lại không tạo tồn kho hỏng ảo
        }

        /// <summary>
        /// TC09_B: Kiểm tra khi hoàn tất phiếu nhập kho thu hồi khách hàng (RMA),
        /// hàng hỏng (RejectedQuantity) được thu hồi và đưa vào khu cách ly kho (QuantityDamaged).
        /// </summary>
        [Fact]
        public async Task TC09_CompleteReceiptAsync_WithCustomerReturn_ShouldRouteDamagedToQuantityDamaged()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var receipt = new InventoryReceipt
            {
                Id = 16,
                ReceiptCode = "IR-RMA-DAMAGED",
                WarehouseId = 1,
                SupplierId = null, // Thu hồi khách hàng không có SupplierId
                Note = "Thu hồi khách hàng theo RMA RET-20260901-001",
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 16,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                ExpectedQuantity = 20,
                AcceptedQuantity = 15,
                RejectedQuantity = 5,
                RejectReason = "5 hộp dập hỏng do vận chuyển"
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(16, receivedById: 1, note: "Nhập hàng thu hồi từ khách");

            // Assert
            success.Should().BeTrue();

            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityAvailable.Should().Be(15);
            inv.QuantityDamaged.Should().Be(5);     // RMA hàng hỏng đưa vào kho cách ly
        }

        /// <summary>
        /// TC09_C: Khi hàng bị từ chối 100% tại dock tiếp nhận, không yêu cầu BatchId,
        /// không sinh bản ghi tồn kho và cập nhật RejectedQuantity lên PO.
        /// </summary>
        [Fact]
        public async Task TC09_CompleteReceiptAsync_When100PercentRejected_ShouldNotCreateInventoryOrRequireBatch()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var receipt = new InventoryReceipt
            {
                Id = 17,
                ReceiptCode = "IR-100PCT-REJECTED",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 17,
                VariantId = 1,
                BatchId = null, // BatchId null vì từ chối 100% tại dock
                UoMId = 1,
                PurchaseOrderDetailId = 1,
                ExpectedQuantity = 50,
                AcceptedQuantity = 0,
                RejectedQuantity = 50,
                RejectReason = "Hàng sai quy cách chất lượng hoàn toàn"
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(17, receivedById: 1, note: "Từ chối toàn bộ 50 đơn vị");

            // Assert
            success.Should().BeTrue();

            var inv = await context.WarehouseInventories.FirstOrDefaultAsync(x => x.WarehouseId == 1 && x.VariantId == 1);
            inv.Should().BeNull(); // Không tạo bản ghi tồn kho

            var poDetail = await context.PurchaseOrderDetails.FindAsync(1);
            poDetail!.RejectedQuantity.Should().Be(50);
        }
        #endregion

        #region TC10: TỰ ĐỘNG CHUYỂN TRẠNG THÁI CUSTOMER RETURN KHI NHẬP KHO THU HỒI (RMA LINKING)
        /// <summary>
        /// TC10: Kiểm tra khi hoàn tất Phiếu nhập kho thu hồi có Note chứa mã phiếu trả hàng RET-...,
        /// hệ thống tự động đồng bộ chuyển trạng thái CustomerReturn tương ứng sang Completed.
        /// </summary>
        [Fact]
        public async Task TC10_CompleteReceiptAsync_WithCustomerReturnCodeInNote_ShouldAutoCompleteReturn()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);
            var now = DateTime.UtcNow;

            var customerReturn = new CustomerReturn
            {
                Id = 20,
                ReturnCode = "RET-20260901-ABC123",
                OrderId = 1,
                CustomerId = 1,
                WarehouseId = 1,
                Status = CustomerReturnStatus.Inspecting,
                ReturnDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.CustomerReturns.Add(customerReturn);

            var receipt = new InventoryReceipt
            {
                Id = 25,
                ReceiptCode = "IR-RMA-RECLAIM",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                Note = "Nhập kho thu hồi theo phiếu trả hàng RET-20260901-ABC123",
                CreatedAt = now,
                UpdatedAt = now
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 25,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                ExpectedQuantity = 5,
                AcceptedQuantity = 5,
                RejectedQuantity = 0
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(25, receivedById: 1, note: null);

            // Assert
            success.Should().BeTrue();

            var updatedReturn = await context.CustomerReturns.FindAsync(20);
            updatedReturn!.Status.Should().Be(CustomerReturnStatus.Completed);
        }
        #endregion

        #region TC11: DUNG SAI HOÀN TẤT NHẬN HÀNG NÔNG SẢN (PO TOLERANCE 5%)
        /// <summary>
        /// TC11: Kiểm tra quy tắc dung sai nông sản tươi 5% (ví dụ đặt 100 hộp nhận 96 hộp >= 95%),
        /// Đơn mua hàng PO vẫn được ghi nhận hoàn tất chu trình (Status = Completed).
        /// </summary>
        [Fact]
        public async Task TC11_CompleteReceiptAsync_WithPOTolerance_ShouldCompletePOWhenReceivedAtLeast95Percent()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var receipt = new InventoryReceipt
            {
                Id = 30,
                ReceiptCode = "IR-PO-TOLERANCE",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 30,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                PurchaseOrderDetailId = 1, // PO đặt 100 hộp
                ExpectedQuantity = 100,
                AcceptedQuantity = 96,     // Nhận thực tế 96 hộp (96% >= 95% threshold)
                RejectedQuantity = 0
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(30, receivedById: 1, note: "Nông sản tươi cân đo thực tế 96 hộp");

            // Assert
            success.Should().BeTrue();

            var po = await context.PurchaseOrders.FindAsync(1);
            po!.Status.Should().Be(PurchaseOrderStatus.Completed);
        }

        /// <summary>
        /// TC12: Chốt sổ phiếu nhập tự động tính toán CBM, Cân nặng và gắn Cảnh báo sức chứa nếu kho chạm ngưỡng.
        /// </summary>
        [Fact]
        public async Task TC12_CompleteReceiptAsync_ShouldCalculatePhysicalMetricsAndAppendCapacityWarning()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var wh = await context.Warehouses.FindAsync(1);
            wh!.TotalCapacityCbm = 1m; // Sức chứa rất nhỏ: 1 CBM
            wh.WarningThresholdPercent = 50;

            var variant = await context.ProductVariants.FindAsync(1);
            variant!.GrossWeightKg = 2.5m;
            variant.LengthCm = 50;
            variant.WidthCm = 40;
            variant.HeightCm = 50; // 50x40x50 / 1,000,000 = 0.1 CBM / unit
            variant.UnitCbm = 0.1m;

            var receipt = new InventoryReceipt
            {
                Id = 40,
                ReceiptCode = "IR-CAPACITY-WARN",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 2,
                InventoryReceiptId = 40,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                ExpectedQuantity = 15,
                AcceptedQuantity = 15, // 15 units * 0.1 CBM = 1.5 CBM > 1 CBM max!
                RejectedQuantity = 0
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(40, receivedById: 1, note: "Kiểm đếm xe tải lớn");

            // Assert
            success.Should().BeTrue();

            var completedReceipt = await context.InventoryReceipts
                .Include(r => r.Details)
                .FirstOrDefaultAsync(r => r.Id == 40);

            completedReceipt.Should().NotBeNull();
            var detail = completedReceipt!.Details.First();
            detail.CalculatedCbm.Should().Be(1.5m);
            detail.ActualWeightKg.Should().Be(37.5m); // 15 * 2.5
            completedReceipt.Note.Should().Contain("[CẢNH BÁO QUÁ TẢI CBM]");
        }

        /// <summary>
        /// TC13: Chốt sổ phiếu nhập chạm ngưỡng cảnh báo lấp đầy (WarningThreshold) gắn Cảnh báo sức chứa sắp đầy.
        /// </summary>
        [Fact]
        public async Task TC13_CompleteReceiptAsync_WhenReachingWarningThreshold_ShouldAppendThresholdWarning()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var wh = await context.Warehouses.FindAsync(1);
            wh!.TotalCapacityCbm = 10m; // Sức chứa: 10 CBM
            wh.WarningThresholdPercent = 80;

            var variant = await context.ProductVariants.FindAsync(1);
            variant!.LengthCm = 50;
            variant.WidthCm = 40;
            variant.HeightCm = 25; // 50x40x25 / 1,000,000 = 0.05 CBM
            variant.UnitCbm = 0.05m;

            var receipt = new InventoryReceipt
            {
                Id = 45,
                ReceiptCode = "IR-THRESHOLD-WARN",
                WarehouseId = 1,
                SupplierId = 1,
                Status = InventoryReceiptStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            receipt.Details.Add(new InventoryReceiptDetail
            {
                Id = 3,
                InventoryReceiptId = 45,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                ExpectedQuantity = 170,
                AcceptedQuantity = 170, // 170 * 0.05 CBM = 8.5 CBM (85% >= 80% threshold, < 100%)
                RejectedQuantity = 0
            });
            context.InventoryReceipts.Add(receipt);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            // Act
            var success = await service.CompleteReceiptAsync(45, receivedById: 1, note: null);

            // Assert
            success.Should().BeTrue();

            var completedReceipt = await context.InventoryReceipts.FindAsync(45);
            completedReceipt!.Note.Should().Contain("[CẢNH BÁO SỨC CHỨA]: Kho sắp đầy");
            completedReceipt.Note.Should().Contain("85.0% dung tích");
        }

        /// <summary>
        /// TC14: Tạo mới phiếu nhập kho lưu vết chính xác Khối lượng cân thực tế (ActualWeightKg) và Thể tích (CalculatedCbm).
        /// </summary>
        [Fact]
        public async Task TC14_CreateReceiptAsync_WithActualWeightAndCalculatedCbm_ShouldPersistDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var variant = await context.ProductVariants.FindAsync(1);
            variant!.GrossWeightKg = 12.5m;
            variant.UnitCbm = 0.04m;
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Note = "Nhập hàng cân tại cổng kho",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new InventoryReceiptDetailCreateDto
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ExpectedQuantity = 50,
                        AcceptedQuantity = 48,
                        RejectedQuantity = 2,
                        RejectReason = "2 thùng bị móp",
                        ActualWeightKg = 600m,
                        CalculatedCbm = 2.0m
                    }
                }
            };

            // Act
            var receiptId = await service.CreateAsync(createDto);
            var created = await service.GetByIdAsync(receiptId);

            // Assert
            created.Should().NotBeNull();
            created.Details.Should().HaveCount(1);
            var detail = created.Details.First();
            detail.ActualWeightKg.Should().Be(600m);
            detail.CalculatedCbm.Should().Be(2.0m);
        }
        #endregion

        #region TC13: SCM SHIELD - CHẶN NHẬP TỪ NCC VÀO KHO KHÔNG PHẢI KHO TỔNG
        /// <summary>
        /// TC13: SCM Rule - Nhập kho từ Nhà cung cấp (SupplierId != null) bắt buộc phải nhập vào Kho Tổng.
        /// Chặn các kho bán lẻ hoặc trạm trung chuyển tiếp nhận trực tiếp từ NCC.
        /// </summary>
        [Fact]
        public async Task CreateAsync_SupplierReceiptToNonMasterHub_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var retailWh = new Warehouse
            {
                Id = 3,
                Code = "WH-RETAIL",
                Name = "Kho Bán Lẻ Tân Bình",
                WarehouseType = WarehouseTypeConstants.Retail,
                IsActive = true
            };
            context.Warehouses.Add(retailWh);
            await context.SaveChangesAsync();

            var service = new InventoryReceiptService(context, _mapper);

            var createDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 3,
                SupplierId = 1,
                Note = "Nhập trực tiếp từ NCC",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new InventoryReceiptDetailCreateDto
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ExpectedQuantity = 100,
                        AcceptedQuantity = 100
                    }
                }
            };

            // Act & Assert
            var act = () => service.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Hàng hóa nhập từ Nhà cung cấp chỉ được phép nhập vào Kho Tổng*");
        }
        #endregion
    }
}
