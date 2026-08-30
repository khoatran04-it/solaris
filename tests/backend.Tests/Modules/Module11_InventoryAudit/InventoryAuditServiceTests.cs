using AutoMapper;
using backend.DTOs.InventoryAuditDTOs;
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

namespace backend.Tests.Modules.Module11_InventoryAudit
{
    /// <summary>
    /// ============================================================================
    /// 📋 MODULE 11: INVENTORY AUDIT & STOCKTAKE RECONCILIATION
    /// 🧪 TEST SUITE: InventoryAuditServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Kiểm kê kho hàng:
    /// - Phân trang, tìm kiếm mã đợt, lọc theo Kho, Trạng thái, Loại kiểm kê, Khoảng ngày
    /// - Lấy chi tiết đợt kiểm kê kèm danh sách chi tiết (Details) đã làm phẳng AutoMapper
    /// - Khởi tạo đợt kiểm kê (Full / Cycle / Spot) và tự động chụp Snapshot số dư tồn kho
    /// - Nộp kết quả kiểm đếm thực tế (Blind Count Submit) và tự động tính toán chênh lệch
    /// - Phê duyệt chốt kiểm kê (Approve & Reconcile): Tự động sinh Phiếu điều chỉnh, cập nhật tồn kho và ghi sổ cái
    /// - Khiên an toàn (Safety Shields): Chặn nộp kết quả khi đã chốt, chặn hủy đợt kiểm kê đã hoàn tất
    /// </summary>
    public class InventoryAuditServiceTests
    {
        private readonly IMapper _mapper;

        public InventoryAuditServiceTests()
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
                Username = "auditor1",
                FullName = "Nguyễn Kiểm Kê",
                Email = "auditor@solaris.vn",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                IsActive = true
            });

            context.IAUsers.Add(new IAUser
            {
                Id = 2,
                CitizenId = "001200005678",
                Username = "manager1",
                FullName = "Trần Quản Lý Kho",
                Email = "manager@solaris.vn",
                PhoneNumber = "0909999999",
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

            context.Warehouses.Add(new Warehouse
            {
                Id = 2,
                Code = "WH-HCM-01",
                Name = "Kho Nam Sài Gòn",
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
                IsActive = true,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 1, UoMId = 1, Price = 50000, IsDefault = true }
                }
            });

            context.ProductVariants.Add(new ProductVariant
            {
                Id = 2,
                ProductId = 1,
                Code = "SKU-DAUTAY-1KG",
                Name = "Dâu Tây Hộp 1kg",
                IsActive = true,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 2, VariantId = 2, UoMId = 1, Price = 95000, IsDefault = true }
                }
            });

            // Seed Batch
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-2026-001",
                VariantId = 1,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-5),
                ExpiryDate = DateTime.UtcNow.AddDays(15),
                IsActive = true
            });

            context.ProductBatches.Add(new ProductBatch
            {
                Id = 2,
                BatchCode = "BATCH-2026-002",
                VariantId = 2,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-5),
                ExpiryDate = DateTime.UtcNow.AddDays(20),
                IsActive = true
            });

            // Seed Warehouse Inventory
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 0
            });

            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 2,
                WarehouseId = 1,
                VariantId = 2,
                BatchId = 2,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 0
            });

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: GetPagedAsync Filters & Search
        [Fact]
        public async Task TC01_GetPagedAsync_WithFiltersAndSearch_ShouldReturnCorrectData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.InventoryAudits.Add(new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                AuditorId = 1,
                AuditType = InventoryAuditType.Full,
                Status = InventoryAuditStatus.InProgress,
                AuditDate = DateTime.UtcNow.AddDays(-1),
                Note = "Kiểm kê định kỳ tháng 8"
            });

            context.InventoryAudits.Add(new InventoryAudit
            {
                Id = 2,
                AuditCode = "AUD-20260830-002",
                WarehouseId = 2,
                AuditorId = 1,
                AuditType = InventoryAuditType.Spot,
                Status = InventoryAuditStatus.Completed,
                AuditDate = DateTime.UtcNow,
                Note = "Kiểm kê đột xuất hàng dập"
            });

            await context.SaveChangesAsync();
            var service = new InventoryAuditService(context, _mapper);

            // Act
            var resultAll = await service.GetPagedAsync(null, null, null, null, null, null, 1, 10);
            var resultFiltered = await service.GetPagedAsync("đột xuất", 2, (int)InventoryAuditStatus.Completed, null, null, null, 1, 10);

            // Assert
            resultAll.TotalRecords.Should().Be(2);
            resultFiltered.TotalRecords.Should().Be(1);
            resultFiltered.Items.First().AuditCode.Should().Be("AUD-20260830-002");
        }
        #endregion

        #region TC02: GetByIdAsync Enriched AutoMapper Flattening
        [Fact]
        public async Task TC02_GetByIdAsync_WhenExists_ShouldReturnEnrichedDtoWithDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 10,
                AuditCode = "AUD-20260830-010",
                WarehouseId = 1,
                AuditorId = 1,
                ApprovedById = 2,
                AuditType = InventoryAuditType.Full,
                Status = InventoryAuditStatus.PendingApproval,
                AuditDate = DateTime.UtcNow,
                TotalSystemQty = 100,
                TotalActualQty = 95,
                TotalVarianceQty = -5,
                TotalVarianceAmount = -250000,
                Details = new List<InventoryAuditDetail>
                {
                    new InventoryAuditDetail
                    {
                        Id = 100,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        SystemQuantity = 100,
                        ActualQuantity = 95,
                        VarianceQuantity = -5,
                        UnitPrice = 50000,
                        VarianceAmount = -250000,
                        ReasonNote = "Hao hụt tự nhiên"
                    }
                }
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(10);

            // Assert
            result.Should().NotBeNull();
            result.AuditCode.Should().Be("AUD-20260830-010");
            result.WarehouseName.Should().Be("Tổng Kho Hà Nội");
            result.AuditorName.Should().Be("Nguyễn Kiểm Kê");
            result.ApprovedByName.Should().Be("Trần Quản Lý Kho");
            result.Details.Should().HaveCount(1);
            result.Details.First().VariantName.Should().Be("Dâu Tây Hộp 500g");
            result.Details.First().VariantCode.Should().Be("SKU-DAUTAY-500G");
            result.Details.First().BatchCode.Should().Be("BATCH-2026-001");
            result.Details.First().UoMName.Should().Be("Hộp 500g");
        }
        #endregion

        #region TC03: GetByIdAsync When Not Exists
        [Fact]
        public async Task TC03_GetByIdAsync_WhenNotExists_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new InventoryAuditService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(999));
        }
        #endregion

        #region TC04: CreateAsync Full Audit Snapshots All Warehouse Inventories
        [Fact]
        public async Task TC04_CreateAsync_FullAudit_ShouldSnapshotAllWarehouseInventories()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryAuditService(context, _mapper);

            var dto = new InventoryAuditCreateDto
            {
                WarehouseId = 1,
                AuditType = InventoryAuditType.Full,
                AuditorId = 1,
                Note = "Kiểm kê toàn bộ tổng kho"
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            var savedAudit = await context.InventoryAudits
                .Include(a => a.Details)
                .FirstOrDefaultAsync(a => a.Id == newId);

            savedAudit.Should().NotBeNull();
            savedAudit!.Status.Should().Be(InventoryAuditStatus.InProgress);
            savedAudit.TotalSystemQty.Should().Be(150); // 100 + 50
            savedAudit.TotalActualQty.Should().Be(0);
            savedAudit.TotalVarianceQty.Should().Be(-150);
            savedAudit.Details.Should().HaveCount(2);

            var line1 = savedAudit.Details.FirstOrDefault(d => d.VariantId == 1);
            line1.Should().NotBeNull();
            line1!.SystemQuantity.Should().Be(100);
            line1.ActualQuantity.Should().Be(0);
            line1.VarianceQuantity.Should().Be(-100);
            line1.UnitPrice.Should().Be(50000);
        }
        #endregion

        #region TC05: CreateAsync Cycle/Spot Audit Snapshots Specific Items Only
        [Fact]
        public async Task TC05_CreateAsync_CycleOrSpotAudit_WithSpecificItems_ShouldSnapshotOnlySpecificVariants()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryAuditService(context, _mapper);

            var dto = new InventoryAuditCreateDto
            {
                WarehouseId = 1,
                AuditType = InventoryAuditType.Spot,
                AuditorId = 1,
                Note = "Kiểm tra đột xuất SKU dâu tây 500g",
                SpecificItems = new List<InventoryAuditDetailCreateDto>
                {
                    new InventoryAuditDetailCreateDto { VariantId = 1, BatchId = 1, UoMId = 1 }
                }
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            var savedAudit = await context.InventoryAudits
                .Include(a => a.Details)
                .FirstOrDefaultAsync(a => a.Id == newId);

            savedAudit.Should().NotBeNull();
            savedAudit!.TotalSystemQty.Should().Be(100);
            savedAudit.Details.Should().HaveCount(1);
            savedAudit.Details.First().VariantId.Should().Be(1);
        }
        #endregion

        #region TC06: CreateAsync When Warehouse Not Found
        [Fact]
        public async Task TC06_CreateAsync_WhenWarehouseNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new InventoryAuditService(context, _mapper);

            var dto = new InventoryAuditCreateDto
            {
                WarehouseId = 999,
                AuditType = InventoryAuditType.Full
            };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto));
        }
        #endregion

        #region TC07: SubmitCountAsync Blind Count Calculation & PendingApproval
        [Fact]
        public async Task TC07_SubmitCountAsync_WhenValid_ShouldCalculateVariancesAndSetPendingApproval()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                AuditorId = 1,
                Status = InventoryAuditStatus.InProgress,
                TotalSystemQty = 150,
                Details = new List<InventoryAuditDetail>
                {
                    new InventoryAuditDetail
                    {
                        Id = 10,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        SystemQuantity = 100,
                        UnitPrice = 50000
                    },
                    new InventoryAuditDetail
                    {
                        Id = 20,
                        VariantId = 2,
                        BatchId = 2,
                        UoMId = 1,
                        SystemQuantity = 50,
                        UnitPrice = 95000
                    }
                }
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            var submitDto = new InventoryAuditSubmitCountDto
            {
                Items = new List<InventoryAuditItemCountDto>
                {
                    new InventoryAuditItemCountDto { DetailId = 10, ActualQuantity = 95, ReasonNote = "Hao hụt 5 hộp dập nát" },
                    new InventoryAuditItemCountDto { DetailId = 20, ActualQuantity = 52, ReasonNote = "Thừa 2 hộp do giao nhầm lô" }
                },
                Note = "Đã hoàn thành kiểm đếm thực tế ngoài kho"
            };

            // Act
            var success = await service.SubmitCountAsync(1, submitDto);

            // Assert
            success.Should().BeTrue();

            var updatedAudit = await context.InventoryAudits
                .Include(a => a.Details)
                .FirstOrDefaultAsync(a => a.Id == 1);

            updatedAudit!.Status.Should().Be(InventoryAuditStatus.PendingApproval);
            updatedAudit.TotalActualQty.Should().Be(147); // 95 + 52
            updatedAudit.TotalVarianceQty.Should().Be(-3);  // -5 + 2
            updatedAudit.TotalVarianceAmount.Should().Be((-5 * 50000) + (2 * 95000)); // -250,000 + 190,000 = -60,000

            var line1 = updatedAudit.Details.First(d => d.Id == 10);
            line1.ActualQuantity.Should().Be(95);
            line1.VarianceQuantity.Should().Be(-5);
            line1.ReasonNote.Should().Be("Hao hụt 5 hộp dập nát");

            var line2 = updatedAudit.Details.First(d => d.Id == 20);
            line2.ActualQuantity.Should().Be(52);
            line2.VarianceQuantity.Should().Be(2);
        }
        #endregion

        #region TC08: SubmitCountAsync When Status Completed
        [Fact]
        public async Task TC08_SubmitCountAsync_WhenAuditCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                Status = InventoryAuditStatus.Completed
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SubmitCountAsync(1, new InventoryAuditSubmitCountDto()));
        }
        #endregion

        #region TC09: ApproveAndReconcileAsync Creates Adjustment, Updates Stock & Logs Ledger
        [Fact]
        public async Task TC09_ApproveAndReconcileAsync_WithVariance_ShouldCreateAdjustmentAndUpdateStockAndLedger()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                AuditorId = 1,
                Status = InventoryAuditStatus.PendingApproval,
                TotalSystemQty = 150,
                TotalActualQty = 147,
                TotalVarianceQty = -3,
                TotalVarianceAmount = -60000,
                Details = new List<InventoryAuditDetail>
                {
                    new InventoryAuditDetail
                    {
                        Id = 10,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        SystemQuantity = 100,
                        ActualQuantity = 95,
                        VarianceQuantity = -5,
                        UnitPrice = 50000,
                        VarianceAmount = -250000,
                        ReasonNote = "Hao hụt 5 hộp"
                    },
                    new InventoryAuditDetail
                    {
                        Id = 20,
                        VariantId = 2,
                        BatchId = 2,
                        UoMId = 1,
                        SystemQuantity = 50,
                        ActualQuantity = 52,
                        VarianceQuantity = 2,
                        UnitPrice = 95000,
                        VarianceAmount = 190000,
                        ReasonNote = "Thừa 2 hộp"
                    }
                }
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            // Act
            var adjId = await service.ApproveAndReconcileAsync(1, approvedById: 2);

            // Assert
            adjId.Should().BeGreaterThan(0);

            // 1. Audit status updated to Completed
            var completedAudit = await context.InventoryAudits.FindAsync(1);
            completedAudit!.Status.Should().Be(InventoryAuditStatus.Completed);
            completedAudit.ApprovedById.Should().Be(2);
            completedAudit.CompletedDate.Should().NotBeNull();

            // 2. InventoryAdjustment created
            var adjustment = await context.InventoryAdjustments
                .Include(a => a.Details)
                .FirstOrDefaultAsync(a => a.Id == adjId);

            adjustment.Should().NotBeNull();
            adjustment!.AuditId.Should().Be(1);
            adjustment.Status.Should().Be(InventoryAdjustmentStatus.Approved);
            adjustment.Details.Should().HaveCount(2);

            // 3. WarehouseInventories updated
            var inv1 = await context.WarehouseInventories.FirstAsync(i => i.VariantId == 1 && i.BatchId == 1);
            inv1.QuantityAvailable.Should().Be(95); // 100 - 5

            var inv2 = await context.WarehouseInventories.FirstAsync(i => i.VariantId == 2 && i.BatchId == 2);
            inv2.QuantityAvailable.Should().Be(52); // 50 + 2

            // 4. InventoryTransactions recorded
            var txns = await context.InventoryTransactions
                .Where(t => t.ReferenceCode == "AUD-20260830-001")
                .ToListAsync();

            txns.Should().HaveCount(2);
            txns.Should().Contain(t => t.VariantId == 1 && t.Quantity == -5);
            txns.Should().Contain(t => t.VariantId == 2 && t.Quantity == 2);
        }
        #endregion

        #region TC10: ApproveAndReconcileAsync When Status Invalid
        [Fact]
        public async Task TC10_ApproveAndReconcileAsync_WhenStatusInvalid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                Status = InventoryAuditStatus.Draft
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAndReconcileAsync(1, 2));
        }
        #endregion

        #region TC11: CancelAsync When Pending
        [Fact]
        public async Task TC11_CancelAsync_WhenPending_ShouldSetCancelled()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                Status = InventoryAuditStatus.InProgress
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            // Act
            var success = await service.CancelAsync(1, "Hủy kiểm kê do sự cố cúp điện");

            // Assert
            success.Should().BeTrue();
            var cancelled = await context.InventoryAudits.FindAsync(1);
            cancelled!.Status.Should().Be(InventoryAuditStatus.Cancelled);
            cancelled.Note.Should().Contain("Hủy kiểm kê do sự cố cúp điện");
        }
        #endregion

        #region TC12: CancelAsync When Completed Shield
        [Fact]
        public async Task TC12_CancelAsync_WhenCompleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                Status = InventoryAuditStatus.Completed
            };

            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var service = new InventoryAuditService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CancelAsync(1, "Thử hủy phiếu đã chốt"));
        }
        #endregion
    }
}
