using AutoMapper;
using backend.DTOs.ProductBatchDTOs;
using backend.Models;
using backend.Services;
using backend.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Modules.Module05_ProductPricing
{
    /// <summary>
    /// ============================================================================
    /// 📦 MODULE 5: PRODUCT & PRICING
    /// 🧪 UNIT TEST: ProductBatchService (Quản lý Lô Hàng Nông Sản - Batches / Lots)
    /// ============================================================================
    /// </summary>
    public class ProductBatchServiceTests
    {
        private readonly IMapper _mapper;

        public ProductBatchServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region TC01: PHÂN TRANG & TÌM KIẾM ĐA TIÊU CHÍ (MÃ LÔ, BIẾN THỂ, NHÀ CUNG CẤP, TRẠNG THÁI, NGÀY)
        /// <summary>
        /// TC01: Tìm kiếm theo mã lô (BatchCode), lọc theo VariantId, SupplierId, trạng thái IsActive và ngày tạo.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterAndPaginateCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.AddRange(
                new ProductVariant { Id = 1, Code = "SKU-01", Name = "Táo Envy", ProductId = 1 },
                new ProductVariant { Id = 2, Code = "SKU-02", Name = "Cà chua bi", ProductId = 1 }
            );

            context.Suppliers.AddRange(
                new Supplier { Id = 10, Code = "SUP-01", Name = "Nhà vườn Đà Lạt", Phone = "0901234567", Email = "sup1@test.com" },
                new Supplier { Id = 20, Code = "SUP-02", Name = "Hợp tác xã Mộc Châu", Phone = "0901234568", Email = "sup2@test.com" }
            );

            context.ProductBatches.AddRange(
                new ProductBatch
                {
                    Id = 100,
                    BatchCode = "LOT-2026-001",
                    VariantId = 1,
                    SupplierId = 10,
                    ManufactureDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExpiryDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc)
                },
                new ProductBatch
                {
                    Id = 101,
                    BatchCode = "LOT-2026-002",
                    VariantId = 2,
                    SupplierId = 10,
                    ManufactureDate = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
                    ExpiryDate = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc)
                },
                new ProductBatch
                {
                    Id = 102,
                    BatchCode = "LOT-2026-003",
                    VariantId = 1,
                    SupplierId = 20,
                    ManufactureDate = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                    ExpiryDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = false,
                    CreatedAt = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc)
                }
            );
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act 1: Tìm kiếm theo mã lô "002"
            var searchResult = await service.GetPagedAsync("002", null, null, null, null, null, 1, 10);

            // Act 2: Lọc theo VariantId = 1
            var variantFilterResult = await service.GetPagedAsync(null, 1, null, null, null, null, 1, 10);

            // Act 3: Lọc theo SupplierId = 10
            var supplierFilterResult = await service.GetPagedAsync(null, null, 10, null, null, null, 1, 10);

            // Act 4: Lọc theo IsActive = false
            var statusResult = await service.GetPagedAsync(null, null, null, false, null, null, 1, 10);

            // Assert
            searchResult.TotalRecords.Should().Be(1);
            searchResult.Items.First().BatchCode.Should().Be("LOT-2026-002");

            variantFilterResult.TotalRecords.Should().Be(2); // IDs 100 & 102
            supplierFilterResult.TotalRecords.Should().Be(2); // IDs 100 & 101
            statusResult.TotalRecords.Should().Be(1); // ID 102
        }
        #endregion

        #region TC02: LẤY TOÀN BỘ DANH SÁCH LÔ HÀNG (ALL LIST)
        /// <summary>
        /// TC02: Lấy toàn bộ danh sách lô hàng sắp xếp theo thời gian mới nhất.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllBatches()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "SKU-01", Name = "Xoài Cát Chu", ProductId = 1 });
            context.Suppliers.Add(new Supplier { Id = 5, Code = "SUP-05", Name = "HTX Nông Nghiệp", Phone = "0901234567", Email = "htx@test.com" });

            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "LOT-XOAI-01",
                VariantId = 1,
                SupplierId = 5,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 30),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act
            var list = (await service.GetAllListAsync()).ToList();

            // Assert
            list.Should().HaveCount(1);
            list.First().BatchCode.Should().Be("LOT-XOAI-01");
            list.First().VariantId.Should().Be(1);
            list.First().SupplierId.Should().Be(5);
        }
        #endregion

        #region TC03: LẤY CHI TIẾT LÔ HÀNG THEO ID
        /// <summary>
        /// TC03: Lấy chi tiết một lô hàng theo ID và ném KeyNotFoundException nếu ID không tồn tại.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnBatch_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "SKU-01", Name = "Cam Sành", ProductId = 1 });
            context.Suppliers.Add(new Supplier { Id = 5, Code = "SUP-05", Name = "Vườn Cam", Phone = "0901234567", Email = "cam@test.com" });
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 10,
                BatchCode = "LOT-CAM-01",
                VariantId = 1,
                SupplierId = 5,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 20)
            });
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act
            var valid = await service.GetByIdAsync(10);
            var actInvalid = () => service.GetByIdAsync(999);

            // Assert
            valid.Should().NotBeNull();
            valid.BatchCode.Should().Be("LOT-CAM-01");
            valid.VariantId.Should().Be(1);

            await actInvalid.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy lô hàng.*");
        }
        #endregion

        #region TC04: TẠO MỚI LÔ HÀNG & BẮT LỖI VALIDATION (NSX/HSD, TRÙNG MÃ, SAI KHÓA NGOẠI)
        /// <summary>
        /// TC04: Tạo mới lô hàng, ném InvalidOperationException khi ExpiryDate <= ManufactureDate, 
        /// trùng mã lô hoặc VariantId/SupplierId không tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldValidateAndCreateSuccessfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "SKU-01", Name = "Dưa hấu", ProductId = 1 });
            context.Suppliers.Add(new Supplier { Id = 5, Code = "SUP-05", Name = "Vựa Dưa Hấu", Phone = "0901234567", Email = "dua@test.com" });
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "LOT-EXISTING",
                VariantId = 1,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 15)
            });
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act & Assert 1: HSD <= NSX
            var invalidDateDto = new ProductBatchCreateDto
            {
                BatchCode = "LOT-NEW",
                VariantId = 1,
                ManufactureDate = new DateTime(2026, 8, 20),
                ExpiryDate = new DateTime(2026, 8, 10) // HSD nhỏ hơn NSX
            };
            var actDate = () => service.CreateAsync(invalidDateDto);
            await actDate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Hạn sử dụng phải lớn hơn Ngày sản xuất.*");

            // Act & Assert 2: Trùng mã lô (case-insensitive)
            var duplicateDto = new ProductBatchCreateDto
            {
                BatchCode = "lot-existing",
                VariantId = 1,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 20)
            };
            var actDuplicate = () => service.CreateAsync(duplicateDto);
            await actDuplicate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Mã lô hàng 'lot-existing' đã tồn tại trong hệ thống.*");

            // Act & Assert 3: VariantId không tồn tại
            var invalidVariantDto = new ProductBatchCreateDto
            {
                BatchCode = "LOT-NEW1",
                VariantId = 999,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 20)
            };
            var actVariant = () => service.CreateAsync(invalidVariantDto);
            await actVariant.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Biến thể sản phẩm không tồn tại*");

            // Act & Assert 4: SupplierId không tồn tại
            var invalidSupplierDto = new ProductBatchCreateDto
            {
                BatchCode = "LOT-NEW2",
                VariantId = 1,
                SupplierId = 999,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 20)
            };
            var actSupplier = () => service.CreateAsync(invalidSupplierDto);
            await actSupplier.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Nhà cung cấp không tồn tại*");

            // Act & Assert 5: Tạo mới thành công
            var validDto = new ProductBatchCreateDto
            {
                BatchCode = "  LOT-2026-VAL  ",
                VariantId = 1,
                SupplierId = 5,
                ManufactureDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                ExpiryDate = new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            };
            var newId = await service.CreateAsync(validDto);
            newId.Should().BeGreaterThan(0);

            var createdEntity = await context.ProductBatches.FindAsync(newId);
            createdEntity!.BatchCode.Should().Be("LOT-2026-VAL");
            createdEntity.VariantId.Should().Be(1);
            createdEntity.SupplierId.Should().Be(5);
        }
        #endregion

        #region TC05: CẬP NHẬT LÔ HÀNG THÀNH CÔNG VÀ BẮT LỖI TRÙNG MÃ
        /// <summary>
        /// TC05: Cập nhật lô hàng thành công, chặn nếu trùng mã với lô hàng khác hoặc ID không tồn tại.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_AndValidateDates()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductVariants.Add(new ProductVariant { Id = 1, Code = "SKU-01", Name = "Dưa lưới", ProductId = 1 });
            context.ProductBatches.AddRange(
                new ProductBatch { Id = 1, BatchCode = "LOT-01", VariantId = 1, ManufactureDate = new DateTime(2026, 8, 1), ExpiryDate = new DateTime(2026, 8, 20), IsActive = true },
                new ProductBatch { Id = 2, BatchCode = "LOT-02", VariantId = 1, ManufactureDate = new DateTime(2026, 8, 1), ExpiryDate = new DateTime(2026, 8, 20), IsActive = true }
            );
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act 1: Cập nhật hợp lệ (Gia hạn HSD và đổi trạng thái)
            var updateDto = new ProductBatchUpdateDto
            {
                ManufactureDate = new DateTime(2026, 8, 2),
                ExpiryDate = new DateTime(2026, 8, 25),
                IsActive = false
            };
            var updateResult = await service.UpdateAsync(1, updateDto);

            // Act 2: Cập nhật HSD <= NSX
            var invalidDateDto = new ProductBatchUpdateDto
            {
                ManufactureDate = new DateTime(2026, 8, 25),
                ExpiryDate = new DateTime(2026, 8, 20),
                IsActive = true
            };
            var actInvalidDate = () => service.UpdateAsync(1, invalidDateDto);

            // Act 3: Cập nhật ID không tồn tại
            var actNotFound = () => service.UpdateAsync(999, updateDto);

            // Assert
            updateResult.Should().BeTrue();
            var updated = await context.ProductBatches.FindAsync(1);
            updated!.ExpiryDate.Should().Be(new DateTime(2026, 8, 25));
            updated.IsActive.Should().BeFalse();

            await actInvalidDate.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Hạn sử dụng phải lớn hơn Ngày sản xuất.*");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy lô hàng cần sửa.*");
        }
        #endregion

        #region TC06: SAFETY SHIELD - CHẶN XÓA KHI CÒN TỒN KHO HOẶC ĐÃ CÓ PHIẾU NHẬP
        /// <summary>
        /// TC06: Chặn xóa lô hàng nếu đang có tồn kho khả dụng (> 0) hoặc đã phát sinh trong lịch sử phiếu nhập kho.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperationException_WhenHasInventoryOrReceipts()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductBatches.AddRange(
                new ProductBatch { Id = 1, BatchCode = "LOT-WITH-STOCK", VariantId = 1, ManufactureDate = new DateTime(2026, 8, 1), ExpiryDate = new DateTime(2026, 8, 20) },
                new ProductBatch { Id = 2, BatchCode = "LOT-WITH-RECEIPT", VariantId = 1, ManufactureDate = new DateTime(2026, 8, 1), ExpiryDate = new DateTime(2026, 8, 20) }
            );

            context.WarehouseInventories.Add(new WarehouseInventory
            {
                Id = 1,
                WarehouseId = 1,
                VariantId = 1,
                BatchId = 1,
                QuantityAvailable = 50
            });

            context.InventoryReceiptDetails.Add(new InventoryReceiptDetail
            {
                Id = 1,
                InventoryReceiptId = 10,
                VariantId = 1,
                BatchId = 2,
                UoMId = 1,
                ExpectedQuantity = 100,
                AcceptedQuantity = 100
            });
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act & Assert 1: Chặn do có tồn kho
            var actStock = () => service.DeleteAsync(1);
            await actStock.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa lô hàng này vì đang có tồn kho khả dụng trong kho.*");

            // Act & Assert 2: Chặn do có phiếu nhập
            var actReceipt = () => service.DeleteAsync(2);
            await actReceipt.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Không thể xóa lô hàng này vì đã phát sinh trong lịch sử phiếu nhập kho.*");
        }
        #endregion

        #region TC07: XÓA THÀNH CÔNG VÀ CHUYỂN ĐỔI TRẠNG THÁI HOẠT ĐỘNG
        /// <summary>
        /// TC07: Xóa thành công lô hàng không có ràng buộc và kiểm tra Toggle Active.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_And_ToggleActiveAsync_ShouldWorkCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 10,
                BatchCode = "LOT-CLEAN",
                VariantId = 1,
                ManufactureDate = new DateTime(2026, 8, 1),
                ExpiryDate = new DateTime(2026, 8, 20),
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new ProductBatchService(context, _mapper);

            // Act 1: Toggle Active
            await service.ToggleActiveAsync(10);
            var batchAfterToggle = await context.ProductBatches.FindAsync(10);
            batchAfterToggle!.IsActive.Should().BeFalse();

            // Act 2: Xóa thành công
            var deleteResult = await service.DeleteAsync(10);
            var batchAfterDelete = await context.ProductBatches.FindAsync(10);

            // Assert
            deleteResult.Should().BeTrue();
            batchAfterDelete!.IsDeleted.Should().BeTrue();
            batchAfterDelete.DeletedAt.Should().NotBeNull();
        }
        #endregion
    }
}
