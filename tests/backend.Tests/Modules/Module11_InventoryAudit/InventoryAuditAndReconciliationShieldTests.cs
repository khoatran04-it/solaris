using AutoMapper;
using backend.Data;
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
    /// TEST SUITE: InventoryAuditAndReconciliationShieldTests (Đợt 2)
    /// ============================================================================
    /// Kiểm thử tính toàn vẹn số liệu Đối soát chốt ca và Kiểm kê kho hàng:
    /// 1. TC01: Tính giật lùi tồn đầu / tồn cuối cho mốc lịch sử khi có phát sinh sau kỳ
    /// 2. TC02: Tổng hợp toàn diện hàng đang nằm ở ngăn cách ly kiểm định (QuantityQC)
    /// 3. TC03: Phê duyệt kiểm kê bằng ĐVT phụ (Thùng) quy đổi chuẩn xác sang ĐVT cơ sở (Kg)
    /// 4. TC04: Chụp ảnh tồn hệ thống bao gồm cả hàng giữ chỗ, loại trừ thặng dư ảo
    /// </summary>
    public class InventoryAuditAndReconciliationShieldTests
    {
        private readonly IMapper _mapper;

        public InventoryAuditAndReconciliationShieldTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        private static async Task SeedCommonDependenciesAsync(SolarisDbContext context)
        {
            // 1. Kho hàng
            context.Warehouses.Add(new Warehouse
            {
                Id = 1,
                Code = "WH-CENTRAL",
                Name = "Tổng Kho Trung Tâm",
                WarehouseType = WarehouseTypeConstants.MasterHub,
                IsActive = true
            });

            // 2. Nhân viên
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                CitizenId = "001200001234",
                Username = "auditor1",
                FullName = "Trần Kiểm Kê",
                Email = "auditor@solaris.vn",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                IsActive = true
            });

            // 3. Đơn vị tính: KG (Base, Id=1), THUNG (Secondary, Id=2)
            context.UoMs.AddRange(
                new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true },
                new UoM { Id = 2, Code = "THUNG_20KG", Name = "Thùng 20 Kg", IsActive = true }
            );

            // 4. Sản phẩm & Biến thể (Cam Sành, BaseUoM = KG)
            var product = new Product
            {
                Id = 1,
                Code = "PROD-CAM",
                Name = "Cam Sành Hàm Yên",
                BaseUoMId = 1,
                IsActive = true
            };
            context.Products.Add(product);

            var variant = new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Code = "SKU-CAM-KG",
                Name = "Cam Sành Loại 1",
                IsActive = true,
                Prices = new List<ProductVariantPrice>
                {
                    new ProductVariantPrice { Id = 1, VariantId = 1, UoMId = 1, Price = 30000, IsDefault = true },
                    new ProductVariantPrice { Id = 2, VariantId = 1, UoMId = 2, Price = 580000, IsDefault = false }
                }
            };
            context.ProductVariants.Add(variant);

            // 5. Quy tắc quy đổi: 1 Thùng (Id=2) = 20 KG (Id=1)
            context.UoMConversions.Add(new UoMConversion
            {
                Id = 1,
                ProductId = 1,
                FromUoMId = 2,
                ToUoMId = 1,
                ConversionFactor = 20,
                IsActive = true
            });

            // 6. Lô hàng
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-CAM-001",
                VariantId = 1,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-30),
                ExpiryDate = DateTime.UtcNow.AddDays(60),
                IsActive = true
            });

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: Báo cáo đối soát chốt ca mốc lịch sử (Historical Shift Closing)
        [Fact]
        public async Task TC01_HistoricalShiftClosing_WithTransactionsAfterPeriod_CalculatesCorrectOpeningAndClosing()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedCommonDependenciesAsync(context);

            // Tồn kho hiện tại ở thời điểm hiện tại: 90 kg
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 90,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 0
            });

            // Kỳ đối soát cần xem: từ 5 ngày trước đến 2 ngày trước
            var fromDate = DateTime.UtcNow.AddDays(-5).Date;
            var toDate = DateTime.UtcNow.AddDays(-2).Date;

            // Các giao dịch TRONG KỲ:
            // Ngày -4: Nhập hàng +50 kg
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = 1,
                TransactionCode = "TXN-IMPORT-01",
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                Type = TransactionType.Receipt,
                Quantity = 50,
                CreatedAt = fromDate.AddDays(1)
            });
            // Ngày -3: Xuất bán 20 kg
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = 2,
                TransactionCode = "TXN-EXPORT-01",
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                Type = TransactionType.Issue,
                Quantity = 20,
                CreatedAt = fromDate.AddDays(2)
            });
            // Ngày -2: Xuất bán 10 kg
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = 3,
                TransactionCode = "TXN-EXPORT-02",
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                Type = TransactionType.Issue,
                Quantity = 10,
                CreatedAt = fromDate.AddDays(3)
            });

            // Giao dịch SAU KỲ (phát sinh ngày hôm nay, ngoài khoảng đối soát):
            // Xuất bán 30 kg
            context.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = 4,
                TransactionCode = "TXN-EXPORT-LATER",
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                Type = TransactionType.Issue,
                Quantity = 30,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var reconciliationService = new InventoryReconciliationService(context);

            // Act
            var report = await reconciliationService.GetShiftClosingReportAsync(1, fromDate, toDate);

            // Assert
            report.Should().NotBeNull();
            report.Items.Should().HaveCount(1);

            var item = report.Items.First();
            // Tồn cuối kỳ (-2 ngày): 90 - (-30) = 120 kg
            item.ClosingStock.Should().Be(120);
            // Tồn đầu kỳ (-5 ngày): 120 - (+20 phát sinh ròng trong kỳ) = 100 kg
            item.OpeningStock.Should().Be(100);
            item.TotalReceipt.Should().Be(50);
            item.TotalIssue.Should().Be(30);
        }
        #endregion

        #region TC02: Tổng hợp hàng ngăn QC trong Báo cáo đối soát
        [Fact]
        public async Task TC02_ShiftClosingReport_IncludesQuantityQC_InTotalStockAndItemDetails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedCommonDependenciesAsync(context);

            // Tồn kho phân bổ ở cả 4 ngăn: Available=50, Reserved=10, Damaged=5, QC=15
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 50,
                QuantityReserved = 10,
                QuantityDamaged = 5,
                QuantityQC = 15
            });

            await context.SaveChangesAsync();

            var reconciliationService = new InventoryReconciliationService(context);

            // Act
            var report = await reconciliationService.GetShiftClosingReportAsync(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

            // Assert
            report.Should().NotBeNull();
            var item = report.Items.First();

            item.CurrentAvailable.Should().Be(50);
            item.CurrentReserved.Should().Be(10);
            item.CurrentDamaged.Should().Be(5);
            item.CurrentQC.Should().Be(15);
            // Tổng tồn cuối kỳ phải bao gồm cả 15 kg trong ngăn QC: 50 + 10 + 5 + 15 = 80
            item.ClosingStock.Should().Be(80);
        }
        #endregion

        #region TC03: Chốt kiểm kê bằng ĐVT phụ (Thùng) quy đổi chuẩn sang Base UoM (Kg)
        [Fact]
        public async Task TC03_ApproveAndReconcile_WithSecondaryUoM_ConvertsToBaseUoMInInventoryAndTransaction()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedCommonDependenciesAsync(context);

            // Ban đầu kho có 100 kg trong ngăn Available
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

            // Tạo phiếu kiểm kê bằng ĐVT Thùng 20kg (UoMId = 2)
            var audit = new InventoryAudit
            {
                Id = 1,
                AuditCode = "AUD-TEST-UOM-01",
                WarehouseId = 1,
                AuditorId = 1,
                AuditType = InventoryAuditType.Cycle,
                Status = InventoryAuditStatus.PendingApproval,
                AuditDate = DateTime.UtcNow,
                TotalSystemQty = 5, // 5 thùng
                TotalActualQty = 7, // 7 thùng
                TotalVarianceQty = 2, // Thặng dư +2 thùng
                TotalVarianceAmount = 2 * 580000,
                Details = new List<InventoryAuditDetail>
                {
                    new InventoryAuditDetail
                    {
                        Id = 1,
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 2, // Thùng 20kg
                        SystemQuantity = 5,
                        ActualQuantity = 7,
                        VarianceQuantity = 2, // +2 thùng
                        UnitPrice = 580000,
                        VarianceAmount = 1160000
                    }
                }
            };
            context.InventoryAudits.Add(audit);
            await context.SaveChangesAsync();

            var uomService = new UoMConversionService(context, _mapper);
            var auditService = new InventoryAuditService(context, _mapper, uomService);

            // Act: Quản lý kho phê duyệt chốt kiểm kê
            var adjustmentId = await auditService.ApproveAndReconcileAsync(audit.Id, approvedById: 1);

            // Assert
            adjustmentId.Should().BeGreaterThan(0);

            // 1. WarehouseInventory phải được cộng 2 thùng * 20 = 40 kg (thay vì chỉ cộng 2)
            var inventory = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            inventory.QuantityAvailable.Should().Be(140); // 100 ban đầu + 40 quy đổi

            // 2. Sổ cái giao dịch InventoryTransactions phải ghi nhận đúng +40 kg ở ĐVT cơ sở
            var transaction = await context.InventoryTransactions
                .FirstOrDefaultAsync(t => t.ReferenceCode == audit.AuditCode);

            transaction.Should().NotBeNull();
            transaction!.Quantity.Should().Be(40);
            transaction.Type.Should().Be(TransactionType.Adjustment);
        }
        #endregion

        #region TC04: Chụp ảnh số dư tồn kho bao gồm cả hàng giữ chỗ (QuantityReserved)
        [Fact]
        public async Task TC04_CreateAudit_SnapshotsAvailablePlusReserved_PreventsPhantomSurplus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedCommonDependenciesAsync(context);

            // Kho đang có: 80 kg khả dụng + 20 kg đã được khách đặt giữ chỗ (chờ đóng gói)
            // Tổng hàng vật lý thực tế nằm trên kệ là 100 kg.
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 80,
                QuantityReserved = 20,
                QuantityQC = 0,
                QuantityDamaged = 0
            });
            await context.SaveChangesAsync();

            var uomService = new UoMConversionService(context, _mapper);
            var auditService = new InventoryAuditService(context, _mapper, uomService);

            // Act 1: Tạo đợt kiểm kê toàn bộ kho (Full Audit)
            var auditId = await auditService.CreateAsync(new InventoryAuditCreateDto
            {
                WarehouseId = 1,
                AuditType = InventoryAuditType.Full,
                AuditorId = 1,
                Note = "Kiểm kê định kỳ chống thặng dư ảo"
            });

            // Assert Snapshot 1: Hệ thống phải chụp ảnh tồn là 100 kg (Available + Reserved)
            var audit = await context.InventoryAudits
                .Include(a => a.Details)
                .FirstAsync(a => a.Id == auditId);

            audit.TotalSystemQty.Should().Be(100);
            audit.Details.First().SystemQuantity.Should().Be(100);

            // Act 2: Nhân viên đếm thấy trên kệ có đúng 100 kg (vì 20kg giữ chỗ vẫn nằm trên kệ)
            await auditService.SubmitCountAsync(auditId, new InventoryAuditSubmitCountDto
            {
                Items = new List<InventoryAuditItemCountDto>
                {
                    new InventoryAuditItemCountDto
                    {
                        DetailId = audit.Details.First().Id,
                        ActualQuantity = 100,
                        ReasonNote = "Khớp số lượng vật lý trên kệ"
                    }
                }
            });

            // Assert Count 2: Chênh lệch phải bằng 0 (không bị hiểu nhầm là thừa 20kg)
            var updatedAudit = await context.InventoryAudits.FirstAsync(a => a.Id == auditId);
            updatedAudit.TotalVarianceQty.Should().Be(0);

            // Act 3: Phê duyệt chốt kiểm kê
            await auditService.ApproveAndReconcileAsync(auditId, approvedById: 1);

            // Assert Final: Tồn kho Available vẫn là 80, Reserved vẫn là 20 (Không bị lạm phát thặng dư ảo)
            var inventory = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            inventory.QuantityAvailable.Should().Be(80);
            inventory.QuantityReserved.Should().Be(20);
        }
        #endregion
    }
}
