using AutoMapper;
using backend.Data;
using backend.DTOs.InventoryAdjustmentDTOs;
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
    /// TEST SUITE: LedgerAndTransactionShieldTests (Đợt 1)
    /// ============================================================================
    /// Kiểm thử tính toàn vẹn và bất biến của Sổ cái giao dịch kho (InventoryTransaction):
    /// 1. Nghiệm thu điều chuyển kho có phát sinh hàng dập nát:
    ///    - Kho đích ghi nhận TransferIn cho cả hàng đạt (ngăn Khả dụng) và hàng hỏng (ngăn Hàng hỏng).
    ///    - Tuyệt đối KHÔNG ghi số âm Adjustment làm lệch 40kg sổ cái kho đích.
    ///    - Tổng số lượng vào sổ cái = Tổng số lượng vật lý thực tế kho đích.
    /// 2. Điều chỉnh chuyển ngăn nội bộ MoveToDamaged:
    ///    - Chuyển đúng từ QuantityAvailable sang QuantityDamaged.
    ///    - Tổng tồn kho vật lý (Available + Damaged) được bảo toàn nguyên vẹn 100%.
    /// 3. Xuất hủy hàng hỏng DisposeDamaged:
    ///    - Giảm đúng QuantityDamaged và ghi nhận giao dịch xuất hủy hợp lệ.
    /// </summary>
    public class LedgerAndTransactionShieldTests
    {
        private readonly IMapper _mapper;

        public LedgerAndTransactionShieldTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        private static async Task SeedDependenciesAsync(SolarisDbContext context)
        {
            // 1. Hai kho hàng: Kho Xuất (WH1) và Kho Nhận (WH2)
            context.Warehouses.AddRange(
                new Warehouse
                {
                    Id = 1,
                    Code = "WH-CENTRAL",
                    Name = "Kho Tổng Trung Tâm",
                    WarehouseType = WarehouseTypeConstants.MasterHub,
                    IsActive = true
                },
                new Warehouse
                {
                    Id = 2,
                    Code = "WH-RETAIL",
                    Name = "Kho Bán Lẻ Chi Nhánh",
                    WarehouseType = WarehouseTypeConstants.Retail,
                    IsActive = true
                }
            );

            // 2. ĐVT & Sản phẩm
            var uom = new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true };
            context.UoMs.Add(uom);

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
                Code = "VAR-CAM-01",
                Name = "Cam Sành Loại 1",
                IsActive = true
            };
            context.ProductVariants.Add(variant);

            var batch = new ProductBatch
            {
                Id = 1,
                VariantId = 1,
                BatchCode = "BATCH-CAM-001",
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };
            context.ProductBatches.Add(batch);

            // 3. Người dùng
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                Username = "warehouse_manager",
                FullName = "Nguyễn Quản Kho",
                Email = "quankho@solaris.vn",
                PhoneNumber = "0901234567",
                CitizenId = "079090123456",
                PasswordHash = "hashed",
                IsActive = true
            });

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: NGHIỆM THU ĐIỀU CHUYỂN KHO CÓ HÀNG HỎNG BẢO TOÀN CÂN BẰNG SỔ CÁI

        [Fact]
        public async Task InspectAndReceiveTransfer_WhenDamagedItemsPresent_LogsTransferInForBothGoodAndDamaged_EquilibriumMaintained()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Kho nguồn (Kho 1) có sẵn 100 kg
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var transferService = new InventoryTransferService(context, _mapper);

            // Bước 1: Tạo chuyển 50 kg từ Kho 1 sang Kho 2
            var createDto = new InventoryTransferCreateDto
            {
                FromWarehouseId = 1,
                ToWarehouseId = 2,
                CreatedById = 1,
                Note = "Điều chuyển 50kg Cam sang Kho Chi Nhánh",
                Details = new List<InventoryTransferDetailCreateDto>
                {
                    new() { VariantId = 1, BatchId = 1, UoMId = 1, Quantity = 50 }
                }
            };
            var transferId = await transferService.CreateAsync(createDto);

            // Bước 2: Quản lý phê duyệt lệnh chuyển (Approved)
            await transferService.ApproveTransferAsync(transferId, approvedById: 1);

            // Bước 3: Xuất kho chuyển đi (Dispatched)
            await transferService.DispatchTransferAsync(transferId, dispatchedById: 1);

            // Xác minh Kho 1 bị trừ 50kg
            var invWh1 = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            invWh1.QuantityAvailable.Should().Be(50);

            // Bước 3: Kho 2 kiểm đếm nhận hàng: 40 kg nguyên vẹn, 10 kg dập nát khi vận chuyển
            var inspectDto = new InventoryTransferInspectReceiveDto
            {
                Note = "Có 10kg bị va đập khi đi qua đèo",
                Items = new List<InventoryTransferItemInspectDto>
                {
                    new()
                    {
                        DetailId = (await context.InventoryTransferDetails.FirstAsync(d => d.InventoryTransferId == transferId)).Id,
                        ActualReceivedQuantity = 40,
                        DamagedQuantity = 10
                    }
                }
            };

            // Act: Kiểm đếm nhận hàng
            var result = await transferService.InspectAndReceiveTransferAsync(transferId, inspectedById: 1, inspectDto);

            // Assert
            result.Should().BeTrue();

            // 1. Tồn kho thực tế tại Kho 2 (Kho đích)
            var invWh2 = await context.WarehouseInventories.FirstOrDefaultAsync(i => i.WarehouseId == 2 && i.VariantId == 1);
            invWh2.Should().NotBeNull();
            invWh2!.QuantityAvailable.Should().Be(40, "40 kg nguyên vẹn được ghi vào ngăn Khả Dụng");
            invWh2.QuantityDamaged.Should().Be(10, "10 kg dập nát được ghi vào ngăn Hàng Hỏng");
            decimal totalPhysicalStockWh2 = invWh2.QuantityAvailable + invWh2.QuantityDamaged;
            totalPhysicalStockWh2.Should().Be(50, "Tổng tồn kho vật lý thực tế tại Kho 2 phải đủ đúng 50 kg đã nhận");

            // 2. Sổ cái giao dịch kho (InventoryTransactions) tại Kho 2
            var txnsWh2 = await context.InventoryTransactions
                .Where(t => t.WarehouseId == 2 && t.VariantId == 1)
                .ToListAsync();

            txnsWh2.Should().HaveCount(2, "Ghi nhận 2 dòng luồng vào: 1 dòng hàng đạt và 1 dòng hàng hỏng");
            txnsWh2.Should().OnlyContain(t => t.Type == TransactionType.TransferIn,
                "Tất cả giao dịch nhận hàng chuyển kho phải mang Type = TransferIn (luồng nhập)");
            txnsWh2.Should().NotContain(t => t.Type == TransactionType.Adjustment,
                "Tuyệt đối KHÔNG ghi số âm Adjustment gây lệch 40kg sổ cái khi vừa mới nhận hàng vào kho");

            decimal totalLedgerInflowWh2 = txnsWh2.Sum(t => t.Quantity);
            totalLedgerInflowWh2.Should().Be(50,
                "Tổng số lượng ghi sổ cái kho đích (+40 đạt + 10 hỏng = 50 kg) phải KHỚP 100% với tồn kho vật lý");
        }

        #endregion

        #region TC02: ĐIỀU CHỈNH CHUYỂN NGĂN NỘI BỘ MOVETODAMAGED BẢO TOÀN TỔNG TỒN VẬT LÝ

        [Fact]
        public async Task MoveToDamaged_TransfersToDamagedCompartment_WhilePreservingTotalPhysicalStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Kho 1 có sẵn 30 kg Khả dụng
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 30,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var adjustmentService = new InventoryAdjustmentService(context, _mapper);

            // Lập phiếu chuyển 8 kg sang hàng hỏng
            var adj = new InventoryAdjustment
            {
                Id = 101,
                AdjustmentCode = "ADJ-MOVE-DAMAGED",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.Damage,
                CreatedById = 1,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.MoveToDamaged,
                        Quantity = 8,
                        UnitPrice = 45000,
                        TotalAmount = 360000,
                        ReasonDetail = "8 kg dập nát vỏ khi bốc xếp"
                    }
                }
            };
            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            // Act
            var approveResult = await adjustmentService.ApproveAdjustmentAsync(101, approvedById: 1);

            // Assert
            approveResult.Should().BeTrue();

            var inv = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            inv.QuantityAvailable.Should().Be(22, "Tồn khả dụng giảm 8kg (30 - 8 = 22 kg)");
            inv.QuantityDamaged.Should().Be(8, "Tồn hàng hỏng tăng 8kg (0 + 8 = 8 kg)");

            decimal totalPhysicalStock = inv.QuantityAvailable + inv.QuantityReserved + inv.QuantityDamaged;
            totalPhysicalStock.Should().Be(30, "Tổng tồn kho vật lý tại kho vẫn phải bảo toàn nguyên vẹn 30 kg!");
        }

        #endregion

        #region TC03: XUẤT HỦY HÀNG HỎNG DISPOSEDAMAGED TRỪ ĐÚNG HÀNG HỎNG

        [Fact]
        public async Task DisposeDamaged_DecreasesDamagedStock_AndRecordsNegativeLedgerOutflow()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            // Kho 1 có 20 kg Khả dụng và 8 kg Hàng hỏng
            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 20,
                QuantityReserved = 0,
                QuantityQC = 0,
                QuantityDamaged = 8,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var adjustmentService = new InventoryAdjustmentService(context, _mapper);

            // Lập phiếu xuất hủy 5 kg hàng hỏng
            var adj = new InventoryAdjustment
            {
                Id = 102,
                AdjustmentCode = "ADJ-DISPOSE-DAMAGED",
                WarehouseId = 1,
                Status = InventoryAdjustmentStatus.Draft,
                Reason = InventoryAdjustmentReason.Damage,
                CreatedById = 1,
                Details = new List<InventoryAdjustmentDetail>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        AdjustmentType = InventoryAdjustmentType.DisposeDamaged,
                        Quantity = 5,
                        UnitPrice = 45000,
                        TotalAmount = 225000,
                        ReasonDetail = "Tiêu hủy 5 kg cam thối mốc"
                    }
                }
            };
            context.InventoryAdjustments.Add(adj);
            await context.SaveChangesAsync();

            // Act
            var approveResult = await adjustmentService.ApproveAdjustmentAsync(102, approvedById: 1);

            // Assert
            approveResult.Should().BeTrue();

            var inv = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            inv.QuantityAvailable.Should().Be(20, "Tồn khả dụng giữ nguyên 20 kg");
            inv.QuantityDamaged.Should().Be(3, "Tồn hàng hỏng giảm 5 kg (8 - 5 = 3 kg)");

            var txn = await context.InventoryTransactions.FirstAsync(t => t.ReferenceCode == "ADJ-DISPOSE-DAMAGED");
            txn.Quantity.Should().Be(-5, "Giao dịch xuất hủy phải ghi nhận số lượng -5 để giảm trừ tồn kho");
        }

        #endregion
    }
}
