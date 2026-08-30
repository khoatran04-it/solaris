using AutoMapper;
using backend.DTOs.InventoryAdjustmentDTOs;
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
    /// ⚖️ MODULE 11: INVENTORY ADJUSTMENTS & WRITE-OFFS
    /// 🧪 TEST SUITE: InventoryAdjustmentServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Điều chỉnh & Xuất hủy tồn kho:
    /// - Phân trang, tìm kiếm mã phiếu, lọc theo Kho, Trạng thái, Lý do, Khoảng ngày
    /// - Lấy chi tiết phiếu điều chỉnh kèm danh sách chi tiết (Details) đã làm phẳng AutoMapper
    /// - Tạo mới phiếu điều chỉnh ở trạng thái Nháp (Draft) mà KHÔNG làm thay đổi số dư tồn kho
    /// - Validation dữ liệu khi tạo: bắt buộc có dòng chi tiết, số lượng > 0, đơn giá >= 0
    /// - Phê duyệt điều chỉnh (ApproveAdjustment):
    ///   + IncreaseAvailable: Cộng vào Hàng Khả Dụng (+)
    ///   + DecreaseAvailable: Trừ thẳng Hàng Khả Dụng (-)
    ///   + MoveToDamaged: Chuyển từ Khả Dụng sang Hàng Hỏng (Available -> Damaged)
    ///   + DisposeDamaged: Xuất hủy hàng hỏng khỏi kho (- Damaged)
    ///   + Ghi nhận nhật ký sổ cái bất biến (InventoryTransaction) cho từng dòng
    /// - Khiên an toàn (Safety Shields): Chặn duyệt phiếu không ở trạng thái Draft
    /// - Hủy phiếu điều chỉnh (CancelAsync) và Xóa mềm (DeleteAsync)
    /// - Khiên bảo vệ Sổ cái: Nghiêm cấm Hủy hoặc Xóa chứng từ đã được phê duyệt (Approved)
    /// </summary>
    public class InventoryAdjustmentServiceTests
    {
        private readonly IMapper _mapper;

        public InventoryAdjustmentServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Seed
        private static async Task SeedDependenciesAsync(Data.SolarisDbContext context)
        {
            // Seed Users
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                CitizenId = "001200001234",
                Username = "creator1",
                FullName = "Nguyễn Lập Phiếu",
                Email = "creator@solaris.vn",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                IsActive = true
            });

            context.IAUsers.Add(new IAUser
            {
                Id = 2,
                CitizenId = "001200005678",
                Username = "manager1",
                FullName = "Trần Kế Toán Trưởng",
                Email = "accountant@solaris.vn",
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

            // Seed Warehouse Inventory (Initial 4-buckets)
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 10
            });

            // Seed Audit reference
            context.InventoryAudits.Add(new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-20260830-001",
                WarehouseId = 1,
                AuditorId = 1,
                Status = InventoryAuditStatus.Completed
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

            context.InventoryAdjustments.Add(new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.Spoilage,
                CreatedById = 1,
                AdjustmentDate = DateTime.UtcNow.AddDays(-1),
                Note = "Dập nát khi vận chuyển"
            });

            context.InventoryAdjustments.Add(new InventoryAdjustment
            {
                Id = 2,
                AdjustmentCode = "ADJ-20260830-002",
                WarehouseId = 2,
                Status = InventoryAdjustmentStatus.Approved,
                Reason = InventoryAdjustmentReason.Surplus,
                CreatedById = 1,
                AdjustmentDate = DateTime.UtcNow,
                Note = "Thừa sau kiểm kê"
            });

            await context.SaveChangesAsync();
            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var resultAll = await service.GetPagedAsync(null, null, null, null, null, null, 1, 10);
            var resultFiltered = await service.GetPagedAsync("Dập nát", 1, (int)InventoryAdjustmentStatus.Draft, (int)InventoryAdjustmentReason.Spoilage, null, null, 1, 10);

            // Assert
            resultAll.TotalRecords.Should().Be(2);
            resultFiltered.TotalRecords.Should().Be(1);
            resultFiltered.Items.First().AdjustmentCode.Should().Be("ADJ-20260830-001");
        }
        #endregion

        #region TC02: GetByIdAsync Enriched AutoMapper Flattening
        [Fact]
        public async Task TC02_GetByIdAsync_WhenExists_ShouldReturnEnrichedDtoWithDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 10,
                AdjustmentCode = "ADJ-20260830-010",
                WarehouseId = 1,
                AuditId = 1,
                Status = InventoryAdjustmentStatus.Approved,
                Reason = InventoryAdjustmentReason.Spoilage,
                CreatedById = 1,
                ApprovedById = 2,
                AdjustmentDate = DateTime.UtcNow,
                ApprovedDate = DateTime.UtcNow,
                TotalVarianceAmount = 500000,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new InventoryAdjustmentDetail
                    {
                        Id = 100,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.MoveToDamaged,
                        Quantity = 10,
                        UnitPrice = 50000,
                        TotalAmount = 500000,
                        ReasonDetail = "Hàng dập nát"
                    }
                }
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var result = await service.GetByIdAsync(10);

            // Assert
            result.Should().NotBeNull();
            result.AdjustmentCode.Should().Be("ADJ-20260830-010");
            result.WarehouseName.Should().Be("Tổng Kho Hà Nội");
            result.AuditCode.Should().Be("AUD-20260830-001");
            result.CreatedByName.Should().Be("Nguyễn Lập Phiếu");
            result.ApprovedByName.Should().Be("Trần Kế Toán Trưởng");
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
            var service = new InventoryAdjustmentService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(999));
        }
        #endregion

        #region TC04: CreateAsync When Valid Creates Draft Adjustment Without Stock Mutation
        [Fact]
        public async Task TC04_CreateAsync_WhenValid_ShouldCreateDraftAdjustmentWithoutStockChange()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryAdjustmentService(context, _mapper);

            var dto = new InventoryAdjustmentCreateDto
            {
                WarehouseId = 1,
                Reason = InventoryAdjustmentReason.LossTheft,
                CreatedById = 1,
                Note = "Phát hiện mất mát trong ca đêm",
                Details = new List<InventoryAdjustmentDetailCreateDto>
                {
                    new InventoryAdjustmentDetailCreateDto
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.DecreaseAvailable,
                        Quantity = 10,
                        UnitPrice = 50000,
                        ReasonDetail = "Mất cắp 10 hộp"
                    }
                }
            };

            // Act
            var newId = await service.CreateAsync(dto);

            // Assert
            newId.Should().BeGreaterThan(0);

            var savedAdj = await context.InventoryAdjustments
                .Include(a => a.Details)
                .FirstOrDefaultAsync(a => a.Id == newId);

            savedAdj.Should().NotBeNull();
            savedAdj!.Status.Should().Be(InventoryAdjustmentStatus.Draft);
            savedAdj.TotalVarianceAmount.Should().Be(500000); // 10 * 50000
            savedAdj.Details.Should().HaveCount(1);

            // Tồn kho thực tế TUYỆT ĐỐI CHƯA ĐỔI
            var inv = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1 && i.BatchId == 1);
            inv.QuantityAvailable.Should().Be(50);
        }
        #endregion

        #region TC05: CreateAsync Validation Shields
        [Fact]
        public async Task TC05_CreateAsync_ValidationShields_EmptyDetailsOrZeroQuantity_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryAdjustmentService(context, _mapper);

            // 1. Empty details
            var emptyDto = new InventoryAdjustmentCreateDto
            {
                WarehouseId = 1,
                Details = new List<InventoryAdjustmentDetailCreateDto>()
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(emptyDto));

            // 2. Quantity <= 0
            var zeroQtyDto = new InventoryAdjustmentCreateDto
            {
                WarehouseId = 1,
                Details = new List<InventoryAdjustmentDetailCreateDto>
                {
                    new InventoryAdjustmentDetailCreateDto
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        Quantity = 0,
                        UnitPrice = 50000
                    }
                }
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(zeroQtyDto));
        }
        #endregion

        #region TC06: ApproveAdjustmentAsync IncreaseAvailable
        [Fact]
        public async Task TC06_ApproveAdjustmentAsync_IncreaseAvailable_ShouldIncreaseStockAndLogLedger()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.Surplus,
                CreatedById = 1,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new InventoryAdjustmentDetail
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.IncreaseAvailable,
                        Quantity = 20,
                        UnitPrice = 50000,
                        TotalAmount = 1000000,
                        ReasonDetail = "Kiểm đếm dôi dư"
                    }
                }
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var success = await service.ApproveAdjustmentAsync(1, approvedById: 2);

            // Assert
            success.Should().BeTrue();

            var approvedAdj = await context.InventoryAdjustments.FindAsync(1);
            approvedAdj!.Status.Should().Be(InventoryAdjustmentStatus.Approved);
            approvedAdj.ApprovedById.Should().Be(2);
            approvedAdj.ApprovedDate.Should().NotBeNull();

            // Tồn kho khả dụng tăng từ 50 lên 70
            var inv = await context.WarehouseInventories.FirstAsync(i => i.VariantId == 1 && i.BatchId == 1);
            inv.QuantityAvailable.Should().Be(70);

            // Ledger log
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "ADJ-20260830-001");
            txn.Should().NotBeNull();
            txn!.Quantity.Should().Be(20);
            txn.Type.Should().Be(TransactionType.Adjustment);
        }
        #endregion

        #region TC07: ApproveAdjustmentAsync DecreaseAvailable
        [Fact]
        public async Task TC07_ApproveAdjustmentAsync_DecreaseAvailable_ShouldDecreaseStockAndLogLedger()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-002",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.LossTheft,
                CreatedById = 1,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new InventoryAdjustmentDetail
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.DecreaseAvailable,
                        Quantity = 15,
                        UnitPrice = 50000,
                        TotalAmount = 750000,
                        ReasonDetail = "Thất thoát"
                    }
                }
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var success = await service.ApproveAdjustmentAsync(1, approvedById: 2);

            // Assert
            success.Should().BeTrue();

            // Tồn kho khả dụng giảm từ 50 xuống 35
            var inv = await context.WarehouseInventories.FirstAsync(i => i.VariantId == 1 && i.BatchId == 1);
            inv.QuantityAvailable.Should().Be(35);

            // Ledger log âm
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(t => t.ReferenceCode == "ADJ-20260830-002");
            txn.Should().NotBeNull();
            txn!.Quantity.Should().Be(-15);
        }
        #endregion

        #region TC08: ApproveAdjustmentAsync MoveToDamaged
        [Fact]
        public async Task TC08_ApproveAdjustmentAsync_MoveToDamaged_ShouldTransferStockFromAvailableToDamaged()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-003",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.Damage,
                CreatedById = 1,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new InventoryAdjustmentDetail
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.MoveToDamaged,
                        Quantity = 10,
                        UnitPrice = 50000,
                        TotalAmount = 500000,
                        ReasonDetail = "Dập nát bao bì chuyển sang hàng hỏng"
                    }
                }
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var success = await service.ApproveAdjustmentAsync(1, approvedById: 2);

            // Assert
            success.Should().BeTrue();

            // Available giảm từ 50 xuống 40, Damaged tăng từ 10 lên 20
            var inv = await context.WarehouseInventories.FirstAsync(i => i.VariantId == 1 && i.BatchId == 1);
            inv.QuantityAvailable.Should().Be(40);
            inv.QuantityDamaged.Should().Be(20);
        }
        #endregion

        #region TC09: ApproveAdjustmentAsync DisposeDamaged
        [Fact]
        public async Task TC09_ApproveAdjustmentAsync_DisposeDamaged_ShouldDeductFromDamagedBucket()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-004",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.Spoilage,
                CreatedById = 1,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new InventoryAdjustmentDetail
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.DisposeDamaged,
                        Quantity = 5,
                        UnitPrice = 50000,
                        TotalAmount = 250000,
                        ReasonDetail = "Xuất hủy tiêu hủy 5 hộp hỏng"
                    }
                }
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var success = await service.ApproveAdjustmentAsync(1, approvedById: 2);

            // Assert
            success.Should().BeTrue();

            // Available giữ nguyên 50, Damaged giảm từ 10 xuống 5
            var inv = await context.WarehouseInventories.FirstAsync(i => i.VariantId == 1 && i.BatchId == 1);
            inv.QuantityAvailable.Should().Be(50);
            inv.QuantityDamaged.Should().Be(5);
        }
        #endregion

        #region TC10: ApproveAdjustmentAsync When Already Approved
        [Fact]
        public async Task TC10_ApproveAdjustmentAsync_WhenAlreadyApproved_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Approved
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAdjustmentAsync(1, 2));
        }
        #endregion

        #region TC11: CancelAsync When Draft
        [Fact]
        public async Task TC11_CancelAsync_WhenDraft_ShouldCancelWithReason()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var success = await service.CancelAsync(1, "Hủy đề xuất do nhầm lẫn số liệu");

            // Assert
            success.Should().BeTrue();
            var cancelled = await context.InventoryAdjustments.FindAsync(1);
            cancelled!.Status.Should().Be(InventoryAdjustmentStatus.Cancelled);
            cancelled.Note.Should().Contain("Hủy đề xuất do nhầm lẫn số liệu");
        }
        #endregion

        #region TC12: CancelAsync When Approved Shield
        [Fact]
        public async Task TC12_CancelAsync_WhenApproved_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Approved
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CancelAsync(1, "Thử hủy phiếu đã duyệt"));
        }
        #endregion

        #region TC13: DeleteAsync When Draft Soft Delete
        [Fact]
        public async Task TC13_DeleteAsync_WhenDraft_ShouldSoftDelete()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act
            var success = await service.DeleteAsync(1);

            // Assert
            success.Should().BeTrue();
            var deleted = await context.InventoryAdjustments.FindAsync(1);
            deleted!.IsDeleted.Should().BeTrue();
            deleted.DeletedAt.Should().NotBeNull();
        }
        #endregion

        #region TC14: DeleteAsync When Approved Shield
        [Fact]
        public async Task TC14_DeleteAsync_WhenApproved_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var adj = new InventoryAdjustment
            {
                Id = 1,
                AdjustmentCode = "ADJ-20260830-001",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Approved
            };

            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            var service = new InventoryAdjustmentService(context, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1));
        }
        #endregion
    }
}
