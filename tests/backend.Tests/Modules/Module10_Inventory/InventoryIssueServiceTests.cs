using AutoMapper;
using backend.DTOs.InventoryIssueDTOs;
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
    /// MODULE 10: INVENTORY ISSUES & FEFO SMART PICKER
    /// TEST SUITE: InventoryIssueServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Quản lý Phiếu Xuất Kho và Thuật toán đề xuất xuất hàng FEFO:
    /// - Phân trang, tìm kiếm mã phiếu, người nhận, mã đơn hàng, lọc theo Kho, Trạng thái, Khoảng ngày
    /// - Phân quyền truy cập theo danh sách kho được gán (Data-Level RBAC)
    /// - Lấy chi tiết phiếu xuất kèm danh sách mặt hàng (Details) làm phẳng AutoMapper
    /// - Tạo mới phiếu xuất ở trạng thái Pending, tự sinh mã ISS-yyyyMMdd-XXXXXX
    /// - Bắt lỗi validation khi tạo: danh sách dòng hàng rỗng, kho không tồn tại
    /// - Hoàn tất xuất kho (CompleteIssue): TRỪ tồn kho (ưu tiên Reserved), GHI sổ cái Issue, ĐỒNG BỘ tiến độ đơn Order
    /// - Khiên an toàn (Safety Shields): Chặn hoàn tất khi phiếu không ở trạng thái Pending/Picking
    /// - Hủy phiếu xuất kho (CancelIssue) với lý do hủy bắt buộc
    /// - Khiên bảo vệ Sổ cái: Chặn Hủy hoặc Xóa (Soft Delete) phiếu đã chốt sổ (Completed)
    /// - Thuật toán FEFO thông minh (GetSuggestedBatchesAsync): Ưu tiên lô cận date lên đầu, chia nhỏ số lượng cần bốc
    /// </summary>
    public class InventoryIssueServiceTests
    {
        private readonly IMapper _mapper;

        public InventoryIssueServiceTests()
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
                Username = "picker",
                FullName = "Trần Nhặt Hàng",
                Email = "picker@solaris.vn",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                IsActive = true
            });

            // Seed Warehouse
            context.Warehouses.AddRange(
                new Warehouse { Id = 1, Code = "WH-HN-01", Name = "Tổng Kho Hà Nội", IsActive = true },
                new Warehouse { Id = 2, Code = "WH-HCM-01", Name = "Kho Nam Sài Gòn", IsActive = true }
            );

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
            context.UoMs.Add(new UoM { Id = 1, Code = "BOX", Name = "Hộp 500g", IsActive = true });

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

            // Seed Batches (FEFO: Lô 1 hết hạn trước, Lô 2 hết hạn sau)
            context.ProductBatches.AddRange(
                new ProductBatch
                {
                    Id = 1,
                    BatchCode = "BATCH-FEFO-001",
                    VariantId = 1,
                    SupplierId = 1,
                    ManufactureDate = DateTime.UtcNow.AddDays(-10),
                    ExpiryDate = DateTime.UtcNow.AddDays(3), // Cận date
                    IsActive = true
                },
                new ProductBatch
                {
                    Id = 2,
                    BatchCode = "BATCH-FEFO-002",
                    VariantId = 1,
                    SupplierId = 1,
                    ManufactureDate = DateTime.UtcNow.AddDays(-5),
                    ExpiryDate = DateTime.UtcNow.AddDays(20), // Xa hơn
                    IsActive = true
                }
            );

            // Seed Customer Order
            var order = new Order
            {
                Id = 1,
                OrderCode = "ORD-20260830-001",
                CustomerId = 1,
                WarehouseId = 1,
                Status = OrderStatus.Processing,
                TotalAmount = 2500000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            order.Details.Add(new OrderDetail
            {
                Id = 1,
                OrderId = 1,
                VariantId = 1,
                UoMId = 1,
                Quantity = 50,
                IssuedQuantity = 0,
                UnitPrice = 50000,
                TotalPrice = 2500000
            });
            context.Orders.Add(order);

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: GET PAGED ASYNC
        [Fact]
        public async Task TC01_GetPagedAsync_ReturnsPagedIssues_WithFilters_AndRBAC()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryIssues.AddRange(
                new InventoryIssue
                {
                    Id = 1,
                    IssueCode = "ISS-20260830-000001",
                    WarehouseId = 1,
                    IssuedById = 1,
                    IssueDate = DateTime.UtcNow,
                    Status = InventoryIssueStatus.Pending,
                    ReceiverName = "Nguyễn Văn Khách",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new InventoryIssue
                {
                    Id = 2,
                    IssueCode = "ISS-20260830-000002",
                    WarehouseId = 2,
                    IssuedById = 1,
                    IssueDate = DateTime.UtcNow,
                    Status = InventoryIssueStatus.Completed,
                    ReceiverName = "Lê Thị Mua",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act 1: Lấy toàn cục
            var allResult = await service.GetPagedAsync(null, null, null, null, null, 1, 10);
            allResult.TotalRecords.Should().Be(2);

            // Act 2: Lọc RBAC theo Kho 1
            var rbacResult = await service.GetPagedAsync(null, null, null, null, null, 1, 10, new List<int> { 1 });
            rbacResult.TotalRecords.Should().Be(1);
            rbacResult.Items.First().WarehouseId.Should().Be(1);

            // Act 3: Tìm kiếm theo tên người nhận
            var searchResult = await service.GetPagedAsync("Nguyễn Văn Khách", null, null, null, null, 1, 10);
            searchResult.TotalRecords.Should().Be(1);
        }
        #endregion

        #region TC02: GET BY ID ASYNC
        [Fact]
        public async Task TC02_GetByIdAsync_ReturnsIssue_WithFlattenedDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var issue = new InventoryIssue
            {
                Id = 10,
                IssueCode = "ISS-20260830-TEST01",
                OrderId = 1,
                WarehouseId = 1,
                IssuedById = 1,
                Status = InventoryIssueStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            issue.Details.Add(new InventoryIssueDetail
            {
                Id = 1,
                InventoryIssueId = 10,
                OrderDetailId = 1,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                Quantity = 20,
                UnitPrice = 50000,
                TotalPrice = 1000000
            });
            context.InventoryIssues.Add(issue);
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act & Assert 1: Lấy thành công
            var result = await service.GetByIdAsync(10, new List<int> { 1 });
            result.Should().NotBeNull();
            result.IssueCode.Should().Be("ISS-20260830-TEST01");
            result.OrderCode.Should().Be("ORD-20260830-001");
            result.WarehouseName.Should().Be("Tổng Kho Hà Nội");
            result.IssuedByName.Should().Be("Trần Nhặt Hàng");

            result.Details.Should().HaveCount(1);
            var detailDto = result.Details.First();
            detailDto.VariantName.Should().Be("Dâu Tây Hộp 500g");
            detailDto.BatchCode.Should().Be("BATCH-FEFO-001");
            detailDto.Quantity.Should().Be(20);

            // Act & Assert 2: Unauthorized RBAC
            var unauthAct = async () => await service.GetByIdAsync(10, new List<int> { 2 });
            await unauthAct.Should().ThrowAsync<KeyNotFoundException>();
        }
        #endregion

        #region TC03: CREATE ASYNC
        [Fact]
        public async Task TC03_CreateAsync_CreatesIssueInPendingStatus_WithAutoGeneratedCode()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryIssueService(context, _mapper);

            var createDto = new InventoryIssueCreateDto
            {
                OrderId = 1,
                WarehouseId = 1,
                ReceiverName = "Khách Hà Nội",
                ReceiverPhone = "0988888888",
                DeliveryAddress = "Số 10 Phố Huế, Hoàn Kiếm, Hà Nội",
                Note = "Giao giờ hành chính",
                Details = new List<InventoryIssueDetailCreateDto>
                {
                    new()
                    {
                        OrderDetailId = 1,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        Quantity = 30,
                        UnitPrice = 50000
                    }
                }
            };

            // Act
            var newId = await service.CreateAsync(createDto, currentUserId: 1);

            // Assert
            var entity = await context.InventoryIssues.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == newId);
            entity.Should().NotBeNull();
            entity!.Status.Should().Be(InventoryIssueStatus.Pending);
            entity.IssueCode.Should().StartWith("ISS-");
            entity.IssuedById.Should().Be(1);
            entity.Details.Should().HaveCount(1);
            entity.Details.First().TotalPrice.Should().Be(1500000); // 30 * 50,000
        }
        #endregion

        #region TC04: CREATE ASYNC VALIDATION
        [Fact]
        public async Task TC04_CreateAsync_ValidationShields_ThrowsException_WhenInvalidData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryIssueService(context, _mapper);

            // Act & Assert 1: Danh sách details rỗng
            var emptyDetailsDto = new InventoryIssueCreateDto { WarehouseId = 1, Details = new List<InventoryIssueDetailCreateDto>() };
            var act1 = async () => await service.CreateAsync(emptyDetailsDto);
            await act1.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*ít nhất 1 dòng chi tiết*");

            // Act & Assert 2: Kho không tồn tại
            var invalidWhDto = new InventoryIssueCreateDto
            {
                WarehouseId = 999,
                Details = new List<InventoryIssueDetailCreateDto> { new() { VariantId = 1, BatchId = 1, UoMId = 1, Quantity = 10 } }
            };
            var act2 = async () => await service.CreateAsync(invalidWhDto);
            await act2.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Kho hàng*không tồn tại*");
        }
        #endregion

        #region TC05: COMPLETE ISSUE ASYNC
        [Fact]
        public async Task TC05_CompleteIssueAsync_DeductsStock_LogsLedger_AndUpdatesOrderStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Setup tồn kho đã giữ chỗ (Reserved = 50)
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                QuantityReserved = 50,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            var issue = new InventoryIssue
            {
                Id = 1,
                IssueCode = "ISS-20260830-COMPLETE",
                OrderId = 1,
                WarehouseId = 1,
                Status = InventoryIssueStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            issue.Details.Add(new InventoryIssueDetail
            {
                Id = 1,
                InventoryIssueId = 1,
                OrderDetailId = 1,
                VariantId = 1,
                BatchId = 1,
                UoMId = 1,
                Quantity = 50, // Xuất đủ 50/50 của Order
                UnitPrice = 50000,
                TotalPrice = 2500000
            });
            context.InventoryIssues.Add(issue);
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act: Hoàn tất xuất kho
            var success = await service.CompleteIssueAsync(1, issuedById: 1, note: "Đã bàn giao cho shipper GHTK");

            // Assert
            success.Should().BeTrue();

            // 1. Trạng thái phiếu xuất chuyển Completed
            var completedIssue = await context.InventoryIssues.FindAsync(1);
            completedIssue!.Status.Should().Be(InventoryIssueStatus.Completed);
            completedIssue.Note.Should().Be("Đã bàn giao cho shipper GHTK");

            // 2. Tồn kho giữ chỗ bị trừ về 0
            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityReserved.Should().Be(0);
            inv.QuantityAvailable.Should().Be(100);

            // 3. Sổ cái được ghi nhận
            var txn = await context.InventoryTransactions.FirstAsync(x => x.ReferenceCode == "ISS-20260830-COMPLETE");
            txn.Type.Should().Be(TransactionType.Issue);
            txn.Quantity.Should().Be(50);

            // 4. Tiến độ đơn hàng và trạng thái chuyển Shipping
            var od = await context.OrderDetails.FindAsync(1);
            od!.IssuedQuantity.Should().Be(50);

            var order = await context.Orders.FindAsync(1);
            order!.Status.Should().Be(OrderStatus.Shipping);
        }
        #endregion

        #region TC06: COMPLETE ISSUE SAFETY SHIELD
        [Fact]
        public async Task TC06_CompleteIssueAsync_SafetyShields_ThrowsException_WhenInvalidState()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryIssues.Add(new InventoryIssue
            {
                Id = 2,
                IssueCode = "ISS-ALREADY-COMPLETED",
                WarehouseId = 1,
                IssuedById = 1,
                IssueDate = DateTime.UtcNow,
                Status = InventoryIssueStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act & Assert
            var act = async () => await service.CompleteIssueAsync(2, 1, null);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*phải ở trạng thái Chờ xử lý*");
        }
        #endregion

        #region TC07: CANCEL ISSUE ASYNC
        [Fact]
        public async Task TC07_CancelIssueAsync_CancelsPendingIssue_WithMandatoryReason()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryIssues.Add(new InventoryIssue
            {
                Id = 3,
                IssueCode = "ISS-PENDING-CANCEL",
                WarehouseId = 1,
                IssuedById = 1,
                IssueDate = DateTime.UtcNow,
                Status = InventoryIssueStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act 1: Bỏ trống lý do hủy
            var emptyReasonAct = async () => await service.CancelIssueAsync(3, "");
            await emptyReasonAct.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Lý do hủy*không được để trống*");

            // Act 2: Hủy với lý do hợp lệ
            var success = await service.CancelIssueAsync(3, "Khách hàng đổi địa chỉ nhận");
            success.Should().BeTrue();

            var cancelledIssue = await context.InventoryIssues.FindAsync(3);
            cancelledIssue!.Status.Should().Be(InventoryIssueStatus.Cancelled);
            cancelledIssue.CancellationReason.Should().Be("Khách hàng đổi địa chỉ nhận");
        }
        #endregion

        #region TC08: CANCEL & DELETE SAFETY SHIELDS FOR COMPLETED ISSUES
        [Fact]
        public async Task TC08_CancelAndSoftDelete_SafetyShields_BlocksWhenIssueAlreadyCompleted()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryIssues.Add(new InventoryIssue
            {
                Id = 4,
                IssueCode = "ISS-PROTECTED-COMPLETED",
                WarehouseId = 1,
                IssuedById = 1,
                IssueDate = DateTime.UtcNow,
                Status = InventoryIssueStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act & Assert 1: Chặn hủy phiếu đã Completed
            var cancelAct = async () => await service.CancelIssueAsync(4, "Muốn hủy");
            await cancelAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể hủy Phiếu xuất kho đã hoàn tất*");

            // Act & Assert 2: Chặn xóa phiếu đã Completed
            var deleteAct = async () => await service.DeleteAsync(4);
            await deleteAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa Phiếu xuất kho đã hoàn tất*");
        }
        #endregion

        #region TC09: FEFO SMART PICKER ALGORITHM
        [Fact]
        public async Task TC09_GetSuggestedBatchesAsync_FEFOAlgorithm_SortsByExpiryDate_AndCalculatesPickQuantity()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Lô 1: Cận date hơn (hạn 3 ngày nữa), có 30kg
            // Lô 2: Hạn xa hơn (hạn 20 ngày nữa), có 100kg
            context.WarehouseInventories.AddRange(
                new WarehouseInventory
                {
                    WarehouseId = 1,
                    VariantId = 1,
                    BatchId = 1,
                    QuantityAvailable = 30,
                    QuantityReserved = 0
                },
                new WarehouseInventory
                {
                    WarehouseId = 1,
                    VariantId = 1,
                    BatchId = 2,
                    QuantityAvailable = 100,
                    QuantityReserved = 0
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            // Act: Cần xuất 50kg
            var suggestions = await service.GetSuggestedBatchesAsync(warehouseId: 1, variantId: 1, neededQuantity: 50);

            // Assert:
            // Thuật toán FEFO phải gợi ý:
            // - Lô 1 (Cận date nhất): Bốc hết 30kg
            // - Lô 2 (Date sau): Bốc 20kg còn lại
            suggestions.Should().HaveCount(2);
            suggestions[0].BatchId.Should().Be(1);
            suggestions[0].SuggestedPickQuantity.Should().Be(30);

            suggestions[1].BatchId.Should().Be(2);
            suggestions[1].SuggestedPickQuantity.Should().Be(20);
        }
        #endregion

        #region TC10: TỰ ĐỘNG TÍNH TOÁN CBM VÀ TẢI TRỌNG KIỆN HÀNG XUẤT KHO
        /// <summary>
        /// TC10: Tạo phiếu xuất kho tự động tính toán Thể tích CBM và Khối lượng kiện hàng xuất đi dựa trên thông số Master Data của SKU.
        /// </summary>
        [Fact]
        public async Task TC10_CreateIssueAsync_ShouldCalculateTotalCbmAndWeight_FromVariantSpecs()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var variant = await context.ProductVariants.FindAsync(1);
            variant!.LengthCm = 40;
            variant.WidthCm = 30;
            variant.HeightCm = 20; // 40x30x20 / 1,000,000 = 0.024 CBM / unit
            variant.UnitCbm = 0.024m;
            variant.GrossWeightKg = 3.5m; // 3.5 kg / unit
            await context.SaveChangesAsync();

            var service = new InventoryIssueService(context, _mapper);

            var createDto = new InventoryIssueCreateDto
            {
                WarehouseId = 1,
                Note = "Xuất kho bán hàng",
                ReceiverName = "Cửa hàng Chi nhánh 1",
                Details = new List<InventoryIssueDetailCreateDto>
                {
                    new InventoryIssueDetailCreateDto
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        Quantity = 50 // 50 * 0.024 = 1.2 CBM, 50 * 3.5 = 175 kg
                    }
                }
            };

            // Act
            var issueId = await service.CreateAsync(createDto, currentUserId: 1);

            // Assert
            var issue = await context.InventoryIssues
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.Id == issueId);

            issue.Should().NotBeNull();
            issue!.Details.Should().HaveCount(1);
            var detail = issue.Details.First();
            detail.TotalCbm.Should().Be(1.2m);
            detail.TotalWeightKg.Should().Be(175m);
        }
        #endregion
    }
}
