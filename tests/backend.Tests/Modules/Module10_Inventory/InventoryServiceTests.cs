using AutoMapper;
using backend.DTOs.InventoryDTOs;
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
    /// MODULE 10: CORE INVENTORY ENGINE & LEDGER
    /// TEST SUITE: InventoryServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng Core Engine quản lý Két sắt Tồn kho 4 ngăn và Sổ cái Giao dịch:
    /// - Truy vấn tổng hợp (GetAllList), Báo cáo tồn kho đa chiều (GetPaged), Lọc cận date (FEFO), Lọc hết hàng
    /// - Phân quyền hiển thị theo danh sách kho được gán (Data-Level RBAC Authorization)
    /// - Chi tiết tồn kho theo ID và cơ chế bảo mật khi cố tình truy cập kho trái quyền
    /// - Lệnh CỘNG TỒN (IncreaseAvailable): Tăng Available + Ghi sổ cái Receipt
    /// - Lệnh GIỮ CHỖ (ReserveInventory): Chuyển Available -> Reserved + Ghi sổ cái Reserve + Chặn khi thiếu hàng
    /// - Lệnh XUẤT KHO THỰC TẾ (IssueReserved): Trừ Reserved/Available + Ghi sổ cái Issue + Chặn khi thiếu hàng
    /// - Lệnh NHẬN HÀNG HOÀN TRẢ (ReceiveCustomerReturn): Chuyển vào ngăn kiểm định QC + Ghi sổ cái CustomerReturn
    /// - Bắt lỗi validation khi số lượng <= 0
    /// </summary>
    public class InventoryServiceTests
    {
        private readonly IMapper _mapper;

        public InventoryServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Seed
        private static async Task SeedDependenciesAsync(Data.SolarisDbContext context)
        {
            // Seed Warehouses
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

            // Seed UoM & Base Product
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true });

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

            // Seed Batches
            context.ProductBatches.AddRange(
                new ProductBatch
                {
                    Id = 1,
                    BatchCode = "BATCH-2026-001",
                    VariantId = 1,
                    SupplierId = 1,
                    ManufactureDate = DateTime.UtcNow.AddDays(-10),
                    ExpiryDate = DateTime.UtcNow.AddDays(3), // Cận date (< 7 ngày)
                    IsActive = true
                },
                new ProductBatch
                {
                    Id = 2,
                    BatchCode = "BATCH-2026-002",
                    VariantId = 1,
                    SupplierId = 1,
                    ManufactureDate = DateTime.UtcNow.AddDays(-5),
                    ExpiryDate = DateTime.UtcNow.AddDays(30), // An toàn
                    IsActive = true
                }
            );

            await context.SaveChangesAsync();
        }
        #endregion

        #region TC01: GET ALL LIST ASYNC
        [Fact]
        public async Task TC01_GetAllListAsync_ReturnsAllInventories_WithFlattenedDetails_AndAppliesRBAC()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.WarehouseInventories.AddRange(
                new WarehouseInventory
                {
                    Id = 1,
                    WarehouseId = 1,
                    VariantId = 1,
                    BatchId = 1,
                    QuantityAvailable = 100,
                    QuantityReserved = 20,
                    QuantityQC = 5,
                    QuantityDamaged = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new WarehouseInventory
                {
                    Id = 2,
                    WarehouseId = 2,
                    VariantId = 1,
                    BatchId = 2,
                    QuantityAvailable = 50,
                    QuantityReserved = 0,
                    QuantityQC = 0,
                    QuantityDamaged = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryService(context, _mapper);

            // Act 1: Không truyền allowedWarehouseIds (Toàn cục)
            var allResult = (await service.GetAllListAsync(null)).ToList();

            // Assert 1
            allResult.Should().HaveCount(2);
            var item1 = allResult.First(x => x.Id == 1);
            item1.WarehouseName.Should().Be("Tổng Kho Hà Nội");
            item1.VariantCode.Should().Be("SKU-DAUTAY-500G");
            item1.VariantName.Should().Be("Dâu Tây Hộp 500g");
            item1.BaseUoMName.Should().Be("Kilogram");
            item1.BatchCode.Should().Be("BATCH-2026-001");
            item1.SupplierName.Should().Be("Nông Trại Đà Lạt GAP");
            item1.TotalQuantity.Should().Be(127); // 100 + 20 + 5 + 2

            // Act 2: Truyền phân quyền chỉ được xem Kho 1
            var rbacResult = (await service.GetAllListAsync(new List<int> { 1 })).ToList();

            // Assert 2
            rbacResult.Should().HaveCount(1);
            rbacResult[0].WarehouseId.Should().Be(1);
        }
        #endregion

        #region TC02: GET PAGED ASYNC
        [Fact]
        public async Task TC02_GetPagedAsync_SearchAndFilter_Warehouse_ExpiringSoon_OutOfStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.WarehouseInventories.AddRange(
                new WarehouseInventory
                {
                    Id = 1,
                    WarehouseId = 1,
                    VariantId = 1,
                    BatchId = 1,
                    QuantityAvailable = 50,
                    QuantityReserved = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new WarehouseInventory
                {
                    Id = 2,
                    WarehouseId = 1,
                    VariantId = 1,
                    BatchId = 2,
                    QuantityAvailable = 0, // Hết hàng
                    QuantityReserved = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            await context.SaveChangesAsync();

            var service = new InventoryService(context, _mapper);

            // Act 1: Lọc hàng cận date (Lô 1 còn 3 ngày)
            var expiringResult = await service.GetPagedAsync(null, 1, isExpiringSoon: true, isOutOfStock: false, 1, 10);
            expiringResult.TotalRecords.Should().Be(1);
            expiringResult.Items.First().BatchCode.Should().Be("BATCH-2026-001");

            // Act 2: Lọc hết hàng
            var outOfStockResult = await service.GetPagedAsync(null, 1, isExpiringSoon: false, isOutOfStock: true, 1, 10);
            outOfStockResult.TotalRecords.Should().Be(1);
            outOfStockResult.Items.First().BatchCode.Should().Be("BATCH-2026-002");

            // Act 3: Tìm kiếm theo mã SKU
            var searchResult = await service.GetPagedAsync("DAUTAY", null, null, null, 1, 10);
            searchResult.TotalRecords.Should().Be(2);
        }
        #endregion

        #region TC03: GET BY ID ASYNC
        [Fact]
        public async Task TC03_GetByIdAsync_ReturnsDetailedInventory_ThrowsKeyNotFound_WhenNotFoundOrUnauthorized()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 10,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 30,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryService(context, _mapper);

            // Act & Assert 1: Lấy thành công
            var result = await service.GetByIdAsync(10, new List<int> { 1 });
            result.Should().NotBeNull();
            result.Id.Should().Be(10);
            result.QuantityAvailable.Should().Be(30);

            // Act & Assert 2: Bị chặn bởi RBAC (User chỉ có quyền kho 2)
            var unauthAct = async () => await service.GetByIdAsync(10, new List<int> { 2 });
            await unauthAct.Should().ThrowAsync<KeyNotFoundException>();

            // Act & Assert 3: Không tìm thấy ID
            var notFoundAct = async () => await service.GetByIdAsync(999);
            await notFoundAct.Should().ThrowAsync<KeyNotFoundException>();
        }
        #endregion

        #region TC04: INCREASE AVAILABLE ASYNC
        [Fact]
        public async Task TC04_IncreaseAvailableAsync_IncreasesStock_AndLogsTransaction()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryService(context, _mapper);

            // Act 1: Tăng tồn kho cho dòng mới chưa có trong kho
            await service.IncreaseAvailableAsync(warehouseId: 1, variantId: 1, batchId: 1, quantity: 150);

            // Assert 1
            var inv = await context.WarehouseInventories.FirstOrDefaultAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.Should().NotBeNull();
            inv!.QuantityAvailable.Should().Be(150);

            // Kiểm tra Sổ cái giao dịch
            var txn = await context.InventoryTransactions.FirstOrDefaultAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            txn.Should().NotBeNull();
            txn!.Type.Should().Be(TransactionType.Receipt);
            txn.Quantity.Should().Be(150);

            // Act 2: Tăng tiếp vào dòng đã tồn tại
            await service.IncreaseAvailableAsync(warehouseId: 1, variantId: 1, batchId: 1, quantity: 50);

            // Assert 2
            inv = await context.WarehouseInventories.FirstOrDefaultAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv!.QuantityAvailable.Should().Be(200);
        }
        #endregion

        #region TC05: RESERVE INVENTORY ASYNC
        [Fact]
        public async Task TC05_ReserveInventoryAsync_TransfersStockFromAvailableToReserved()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 100,
                QuantityReserved = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryService(context, _mapper);

            // Act 1: Khóa 40kg cho đơn đặt hàng
            await service.ReserveInventoryAsync(warehouseId: 1, variantId: 1, batchId: 1, quantity: 40);

            // Assert 1
            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityAvailable.Should().Be(60);
            inv.QuantityReserved.Should().Be(40);

            var txn = await context.InventoryTransactions.FirstAsync(x => x.Type == TransactionType.Reserve);
            txn.Quantity.Should().Be(40);

            // Act 2 & Assert 2: Khóa vượt quá số dư khả dụng (còn 60 mà đòi khóa 70)
            var overReserveAct = async () => await service.ReserveInventoryAsync(1, 1, 1, 70);
            await overReserveAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không đủ tồn kho khả dụng để giữ chỗ*");
        }
        #endregion

        #region TC06: ISSUE RESERVED ASYNC
        [Fact]
        public async Task TC06_IssueReservedAsync_DeductsReservedAndAvailableStock()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.WarehouseInventories.Add(new WarehouseInventory
            {
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 60,
                QuantityReserved = 40,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new InventoryService(context, _mapper);

            // Act 1: Xuất 40kg (Khớp đúng số lượng đã giữ chỗ)
            await service.IssueReservedAsync(warehouseId: 1, variantId: 1, batchId: 1, quantity: 40);

            // Assert 1
            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityReserved.Should().Be(0);
            inv.QuantityAvailable.Should().Be(60);

            var txn = await context.InventoryTransactions.FirstAsync(x => x.Type == TransactionType.Issue);
            txn.Quantity.Should().Be(40);

            // Act 2: Xuất thêm 20kg (Không có hàng giữ chỗ -> Trừ thẳng vào Available)
            await service.IssueReservedAsync(warehouseId: 1, variantId: 1, batchId: 1, quantity: 20);

            // Assert 2
            inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityAvailable.Should().Be(40);

            // Act 3 & Assert 3: Xuất quá mức tổng tồn kho
            var overIssueAct = async () => await service.IssueReservedAsync(1, 1, 1, 100);
            await overIssueAct.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không đủ tồn kho*");
        }
        #endregion

        #region TC07: RECEIVE CUSTOMER RETURN ASYNC
        [Fact]
        public async Task TC07_ReceiveCustomerReturnAsync_AddsToQCQuarantineBucket()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new InventoryService(context, _mapper);

            // Act: Nhận 10kg hàng khách hoàn trả
            await service.ReceiveCustomerReturnAsync(warehouseId: 1, variantId: 1, batchId: 1, quantity: 10);

            // Assert: Hàng phải vào ngăn QC kiểm định (QuantityQC = 10), QuantityAvailable vẫn = 0
            var inv = await context.WarehouseInventories.FirstAsync(x => x.WarehouseId == 1 && x.VariantId == 1 && x.BatchId == 1);
            inv.QuantityQC.Should().Be(10);
            inv.QuantityAvailable.Should().Be(0);

            var txn = await context.InventoryTransactions.FirstAsync(x => x.Type == TransactionType.CustomerReturn);
            txn.Quantity.Should().Be(10);
        }
        #endregion

        #region TC08: VALIDATION SHIELDS
        [Fact]
        public async Task TC08_Validation_ThrowsArgumentException_WhenNegativeOrZeroQuantity()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            var service = new InventoryService(context, _mapper);

            // Act & Assert
            var act1 = async () => await service.IncreaseAvailableAsync(1, 1, 1, 0);
            await act1.Should().ThrowAsync<ArgumentException>();

            var act2 = async () => await service.ReserveInventoryAsync(1, 1, 1, -5);
            await act2.Should().ThrowAsync<ArgumentException>();

            var act3 = async () => await service.IssueReservedAsync(1, 1, 1, 0);
            await act3.Should().ThrowAsync<ArgumentException>();

            var act4 = async () => await service.ReceiveCustomerReturnAsync(1, 1, 1, -1);
            await act4.Should().ThrowAsync<ArgumentException>();
        }
        #endregion
    }
}
