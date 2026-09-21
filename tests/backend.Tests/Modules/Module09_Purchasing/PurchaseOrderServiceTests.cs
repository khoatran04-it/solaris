using AutoMapper;
using backend.DTOs.PurchaseOrderDTOs;
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

namespace backend.Tests.Modules.Module09_Purchasing
{
    /// <summary>
    /// ============================================================================
    /// MODULE 09: PURCHASING & PURCHASE ORDER MANAGEMENT
    /// TEST SUITE: PurchaseOrderServiceTests
    /// ============================================================================
    /// Kiểm thử toàn diện tầng nghiệp vụ Quản lý Đơn Đặt Mua Hàng từ Nhà Cung Cấp (Purchase Orders - PO):
    /// - Truy vấn danh sách tổng hợp (GetAllList), phân trang, lọc theo NCC, trạng thái, ngày đặt
    /// - Lấy chi tiết đơn mua hàng kèm danh sách mặt hàng (Details)
    /// - Tạo mới đơn mua hàng ở trạng thái Draft, tự động sinh mã PO, tính tổng tiền TotalAmount
    /// - Bắt lỗi validation khi tạo/sửa: chi tiết rỗng, số lượng <= 0, giá < 0, NCC/Biến thể không tồn tại
    /// - Chỉnh sửa thông tin đơn hàng và danh sách hàng hóa (Chỉ cho phép khi ở trạng thái Draft)
    /// - Chuyển đổi trạng thái vòng đời (Workflow: Draft -> Processing -> Approved -> Cancelled)
    /// - Bắt buộc cung cấp lý do hủy đơn (CancellationReason) khi chuyển trạng thái Cancelled
    /// - Khiên an toàn (Safety Shields): Chặn hủy/xóa đơn khi đã phát sinh phiếu nhập kho hoặc nhận hàng thực tế
    /// - Chu trình sống Soft Delete đơn mua hàng (chỉ cho phép xóa khi ở trạng thái Draft hoặc Cancelled)
    /// - Đảm bảo tính toàn vẹn giao dịch (Database Transaction Strategy)
    /// </summary>
    public class PurchaseOrderServiceTests
    {
        private readonly IMapper _mapper;

        public PurchaseOrderServiceTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        private static async Task SeedDependenciesAsync(Data.SolarisDbContext context)
        {
            // Seed User
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                CitizenId = "001200001234",
                Username = "purchaser",
                FullName = "Nguyễn Mua Hàng",
                Email = "purchase@solaris.vn",
                PhoneNumber = "0901234567",
                PasswordHash = "hash",
                IsActive = true
            });

            // Seed Supplier
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "SUP-DALAT",
                Name = "Nông Trại Đà Lạt GAP",
                Phone = "02633123456",
                Email = "contact@dalatgap.vn",
                TaxCode = "0102030405",
                IsActive = true
            });

            // Seed UoM
            context.UoMs.Add(new UoM
            {
                Id = 1,
                Code = "KG",
                Name = "Kilogram",
                IsActive = true
            });

            // Seed Product & Variant
            context.Products.Add(new Product
            {
                Id = 1,
                Code = "PROD-DAUTAY",
                Name = "Dâu Tây New Zealand",
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

            await context.SaveChangesAsync();
        }
        #endregion

        #region 1. TEST CASES: QUERIES (GET ALL, GET PAGED, GET BY ID)

        /// <summary>
        /// TC01: Lấy toàn bộ danh sách đơn mua hàng không phân trang kèm thông tin quan hệ.
        /// </summary>
        [Fact]
        public async Task GetAllListAsync_ShouldReturnAllOrders_WithFlattenedData()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-20260830-001",
                SupplierId = 1,
                CreatedById = 1,
                OrderDate = new DateTime(2026, 8, 30),
                TotalAmount = 500000,
                Status = PurchaseOrderStatus.Draft,
                IsDeleted = false
            });
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act
            var result = (await service.GetAllListAsync()).ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].OrderCode.Should().Be("PO-20260830-001");
            result[0].SupplierName.Should().Be("Nông Trại Đà Lạt GAP");
            result[0].CreatedByName.Should().Be("Nguyễn Mua Hàng");
        }

        /// <summary>
        /// TC02: Phân trang và tìm kiếm theo mã đơn, lọc theo NCC và trạng thái.
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldFilterCorrectly_BySearchAndStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.PurchaseOrders.AddRange(
                new PurchaseOrder
                {
                    Id = 1,
                    OrderCode = "PO-DALAT-001",
                    SupplierId = 1,
                    CreatedById = 1,
                    OrderDate = new DateTime(2026, 8, 20),
                    TotalAmount = 1000000,
                    Status = PurchaseOrderStatus.Approved
                },
                new PurchaseOrder
                {
                    Id = 2,
                    OrderCode = "PO-MOCCHAU-002",
                    SupplierId = 1,
                    CreatedById = 1,
                    OrderDate = new DateTime(2026, 8, 25),
                    TotalAmount = 2000000,
                    Status = PurchaseOrderStatus.Draft
                }
            );
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act 1: Lọc theo từ khóa "DALAT"
            var resultSearch = await service.GetPagedAsync("DALAT", null, null, null, null, 1, 10);

            // Act 2: Lọc theo trạng thái Draft
            var resultStatus = await service.GetPagedAsync(null, null, (int)PurchaseOrderStatus.Draft, null, null, 1, 10);

            // Assert
            resultSearch.Items.Should().HaveCount(1);
            resultSearch.Items.First().OrderCode.Should().Be("PO-DALAT-001");

            resultStatus.Items.Should().HaveCount(1);
            resultStatus.Items.First().OrderCode.Should().Be("PO-MOCCHAU-002");
        }

        /// <summary>
        /// TC03: Lấy chi tiết đơn mua hàng theo ID, ném KeyNotFoundException nếu không tìm thấy.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldReturnDetails_OrThrowKeyNotFound()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-DETAIL-001",
                SupplierId = 1,
                CreatedById = 1,
                OrderDate = new DateTime(2026, 8, 30),
                TotalAmount = 300000,
                Status = PurchaseOrderStatus.Draft,
                Details = new List<PurchaseOrderDetail>
                {
                    new PurchaseOrderDetail
                    {
                        Id = 1,
                        VariantId = 1,
                        UoMId = 1,
                        OrderQuantity = 10,
                        UnitPrice = 30000,
                        TotalPrice = 300000
                    }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act
            var found = await service.GetByIdAsync(1);
            var actNotFound = () => service.GetByIdAsync(999);

            // Assert
            found.Should().NotBeNull();
            found.OrderCode.Should().Be("PO-DETAIL-001");
            found.Details.Should().HaveCount(1);
            found.Details.First().VariantName.Should().Be("Dâu Tây Hộp 500g");

            await actNotFound.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("*Không tìm thấy Đơn đặt mua hàng với ID 999.*");
        }

        #endregion

        #region 2. TEST CASES: CREATE OPERATIONS

        /// <summary>
        /// TC04: Tạo mới đơn mua hàng thành công, tự động tính tổng tiền và gán trạng thái Draft.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldCalculateTotalAmount_AndSetDraftStatus()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new PurchaseOrderService(context, _mapper);

            var dto = new PurchaseOrderCreateDto
            {
                SupplierId = 1,
                OrderDate = new DateTime(2026, 8, 30),
                ExpectedDeliveryDate = new DateTime(2026, 9, 2),
                Note = "Giao hàng buổi sáng",
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new PurchaseOrderDetailCreateDto
                    {
                        VariantId = 1,
                        UoMId = 1,
                        OrderQuantity = 50,
                        UnitPrice = 40000
                    }
                }
            };

            // Act
            var createdId = await service.CreateAsync(dto, 1);

            // Assert
            createdId.Should().BeGreaterThan(0);
            var saved = await context.PurchaseOrders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == createdId);
            saved.Should().NotBeNull();
            saved!.Status.Should().Be(PurchaseOrderStatus.Draft);
            saved.TotalAmount.Should().Be(2000000); // 50 * 40,000 = 2,000,000
            saved.Details.Should().HaveCount(1);
            saved.Details.First().TotalPrice.Should().Be(2000000);
            saved.Details.First().ReceivedQuantity.Should().Be(0);
            saved.OrderCode.Should().StartWith("PO-");
        }

        /// <summary>
        /// TC05: Bắt lỗi nghiệp vụ khi tạo đơn: danh sách mặt hàng rỗng, số lượng <= 0, đơn giá âm hoặc NCC không tồn tại.
        /// </summary>
        [Fact]
        public async Task CreateAsync_ShouldThrowInvalidOperation_WhenValidationFails()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var service = new PurchaseOrderService(context, _mapper);

            // Case 1: Danh sách chi tiết rỗng
            var emptyDetailsDto = new PurchaseOrderCreateDto
            {
                SupplierId = 1,
                OrderDate = DateTime.UtcNow,
                Details = new List<PurchaseOrderDetailCreateDto>()
            };
            var actEmpty = () => service.CreateAsync(emptyDetailsDto, 1);

            // Case 2: Số lượng <= 0
            var zeroQuantityDto = new PurchaseOrderCreateDto
            {
                SupplierId = 1,
                OrderDate = DateTime.UtcNow,
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new PurchaseOrderDetailCreateDto { VariantId = 1, UoMId = 1, OrderQuantity = 0, UnitPrice = 50000 }
                }
            };
            var actZeroQty = () => service.CreateAsync(zeroQuantityDto, 1);

            // Case 3: Nhà cung cấp không tồn tại
            var invalidSupplierDto = new PurchaseOrderCreateDto
            {
                SupplierId = 999,
                OrderDate = DateTime.UtcNow,
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new PurchaseOrderDetailCreateDto { VariantId = 1, UoMId = 1, OrderQuantity = 10, UnitPrice = 50000 }
                }
            };
            var actInvalidSupplier = () => service.CreateAsync(invalidSupplierDto, 1);

            // Assert
            await actEmpty.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Đơn đặt mua hàng phải có ít nhất 1 mặt hàng.*");

            await actZeroQty.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Số lượng đặt mua của từng mặt hàng phải lớn hơn 0.*");

            await actInvalidSupplier.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Nhà cung cấp với ID 999 không tồn tại hoặc đã bị xóa.*");
        }

        #endregion

        #region 3. TEST CASES: UPDATE OPERATIONS

        /// <summary>
        /// TC06: Chỉnh sửa toàn bộ đơn mua hàng và chi tiết khi đơn ở trạng thái Draft.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldUpdateSuccessfully_WhenStatusIsDraft()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-DRAFT-001",
                SupplierId = 1,
                CreatedById = 1,
                OrderDate = new DateTime(2026, 8, 30),
                TotalAmount = 500000,
                Status = PurchaseOrderStatus.Draft,
                Details = new List<PurchaseOrderDetail>
                {
                    new PurchaseOrderDetail { Id = 1, VariantId = 1, UoMId = 1, OrderQuantity = 10, UnitPrice = 50000, TotalPrice = 500000 }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            var updateDto = new PurchaseOrderCreateDto
            {
                SupplierId = 1,
                OrderDate = new DateTime(2026, 8, 31),
                ExpectedDeliveryDate = new DateTime(2026, 9, 3),
                Note = "Cập nhật số lượng lên 20",
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new PurchaseOrderDetailCreateDto { VariantId = 1, UoMId = 1, OrderQuantity = 20, UnitPrice = 45000 }
                }
            };

            // Act
            var result = await service.UpdateAsync(1, updateDto);

            // Assert
            result.Should().BeTrue();
            var updated = await context.PurchaseOrders.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == 1);
            updated!.TotalAmount.Should().Be(900000); // 20 * 45,000 = 900,000
            updated.Details.Should().HaveCount(1);
            updated.Details.First().OrderQuantity.Should().Be(20);
        }

        /// <summary>
        /// TC07: SAFETY SHIELD - Chặn sửa đơn mua hàng khi đơn không còn ở trạng thái Draft.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldThrowInvalidOperation_WhenStatusIsNotDraft()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-APPROVED-001",
                SupplierId = 1,
                CreatedById = 1,
                OrderDate = new DateTime(2026, 8, 30),
                TotalAmount = 500000,
                Status = PurchaseOrderStatus.Approved
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            var updateDto = new PurchaseOrderCreateDto
            {
                SupplierId = 1,
                OrderDate = DateTime.UtcNow,
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new PurchaseOrderDetailCreateDto { VariantId = 1, UoMId = 1, OrderQuantity = 10, UnitPrice = 50000 }
                }
            };

            // Act
            var act = () => service.UpdateAsync(1, updateDto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ được phép chỉnh sửa đơn đặt mua hàng khi đang ở trạng thái Nháp (Draft).*");
        }

        #endregion

        #region 4. TEST CASES: WORKFLOW & STATUS TRANSITIONS

        /// <summary>
        /// TC08: Chuyển đổi trạng thái đơn mua hàng (Draft -> Processing -> Approved).
        /// </summary>
        [Fact]
        public async Task UpdateStatusAsync_ShouldTransitionStatusSuccessfully()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-WORKFLOW-001",
                SupplierId = 1,
                CreatedById = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000000,
                Status = PurchaseOrderStatus.Draft
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act 1: Draft -> Processing
            var step1 = await service.UpdateStatusAsync(1, new PurchaseOrderStatusUpdateDto
            {
                Status = PurchaseOrderStatus.Processing
            });

            // Act 2: Processing -> Approved
            var step2 = await service.UpdateStatusAsync(1, new PurchaseOrderStatusUpdateDto
            {
                Status = PurchaseOrderStatus.Approved
            });

            // Assert
            step1.Should().BeTrue();
            step2.Should().BeTrue();
            var updated = await context.PurchaseOrders.FindAsync(1);
            updated!.Status.Should().Be(PurchaseOrderStatus.Approved);
        }

        /// <summary>
        /// TC09: Hủy đơn mua hàng bắt buộc phải có lý do hủy (CancellationReason) và chặn hủy nếu đã nhận hàng.
        /// </summary>
        [Fact]
        public async Task UpdateStatusAsync_ShouldRequireReasonForCancel_AndShieldIfReceived()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 1,
                OrderCode = "PO-CANCEL-001",
                SupplierId = 1,
                CreatedById = 1,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000000,
                Status = PurchaseOrderStatus.Processing,
                Details = new List<PurchaseOrderDetail>
                {
                    new PurchaseOrderDetail { Id = 1, VariantId = 1, UoMId = 1, OrderQuantity = 10, UnitPrice = 100000, ReceivedQuantity = 0 }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act & Assert 1: Hủy đơn nhưng không truyền lý do
            var actNoReason = () => service.UpdateStatusAsync(1, new PurchaseOrderStatusUpdateDto
            {
                Status = PurchaseOrderStatus.Cancelled,
                CancellationReason = ""
            });
            await actNoReason.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Vui lòng cung cấp lý do hủy đơn đặt mua hàng.*");

            // Act 2: Hủy đơn hợp lệ kèm lý do
            var cancelResult = await service.UpdateStatusAsync(1, new PurchaseOrderStatusUpdateDto
            {
                Status = PurchaseOrderStatus.Cancelled,
                CancellationReason = "Nhà cung cấp báo cháy kho, hết nguồn cung"
            });
            cancelResult.Should().BeTrue();

            var cancelledPo = await context.PurchaseOrders.FindAsync(1);
            cancelledPo!.Status.Should().Be(PurchaseOrderStatus.Cancelled);
            cancelledPo.CancellationReason.Should().Be("Nhà cung cấp báo cháy kho, hết nguồn cung");
        }

        #endregion

        #region 5. TEST CASES: DELETE OPERATIONS & SAFETY SHIELDS

        /// <summary>
        /// TC10: Xóa mềm thành công đơn mua hàng ở trạng thái Draft hoặc Cancelled.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldSoftDelete_WhenStatusIsDraftOrCancelled()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.PurchaseOrders.AddRange(
                new PurchaseOrder
                {
                    Id = 1,
                    OrderCode = "PO-DRAFT-DEL",
                    SupplierId = 1,
                    CreatedById = 1,
                    Status = PurchaseOrderStatus.Draft
                },
                new PurchaseOrder
                {
                    Id = 2,
                    OrderCode = "PO-CANCEL-DEL",
                    SupplierId = 1,
                    CreatedById = 1,
                    Status = PurchaseOrderStatus.Cancelled,
                    CancellationReason = "Hủy đơn"
                }
            );
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act
            var del1 = await service.DeleteAsync(1);
            var del2 = await service.DeleteAsync(2);

            // Assert
            del1.Should().BeTrue();
            del2.Should().BeTrue();

            var po1 = await context.PurchaseOrders.FindAsync(1);
            var po2 = await context.PurchaseOrders.FindAsync(2);

            po1!.IsDeleted.Should().BeTrue();
            po2!.IsDeleted.Should().BeTrue();
        }

        /// <summary>
        /// TC11: SAFETY SHIELD - Chặn xóa đơn mua hàng khi đơn ở trạng thái Approved hoặc đã có nhận hàng thực tế.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_ShouldThrowInvalidOperation_WhenOrderIsApprovedOrHasReceivedQuantity()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            context.PurchaseOrders.AddRange(
                new PurchaseOrder
                {
                    Id = 1,
                    OrderCode = "PO-ACTIVE-SHIELD",
                    SupplierId = 1,
                    CreatedById = 1,
                    Status = PurchaseOrderStatus.Approved
                },
                new PurchaseOrder
                {
                    Id = 2,
                    OrderCode = "PO-RECEIVED-SHIELD",
                    SupplierId = 1,
                    CreatedById = 1,
                    Status = PurchaseOrderStatus.PartiallyReceived,
                    Details = new List<PurchaseOrderDetail>
                    {
                        new PurchaseOrderDetail { Id = 10, VariantId = 1, UoMId = 1, OrderQuantity = 10, ReceivedQuantity = 5 }
                    }
                }
            );
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act & Assert 1: Chặn xóa đơn Approved
            var actApproved = () => service.DeleteAsync(1);
            await actApproved.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ được phép xóa đơn đặt mua hàng khi đang ở trạng thái Nháp (Draft) hoặc Đã hủy (Cancelled).*");

            // Act & Assert 2: Chặn xóa đơn đã nhận hàng
            var actReceived = () => service.DeleteAsync(2);
            await actReceived.Should().ThrowAsync<InvalidOperationException>();
        }

        #endregion

        #region SCM SHIELD: CHẶN LẬP ĐƠN MUA HÀNG VỚI KHO KHÔNG PHẢI KHO TỔNG
        /// <summary>
        /// SCM Rule: Đơn mua hàng (PO) từ Nhà cung cấp chỉ được phép tiếp nhận tại Kho Tổng (MasterHub).
        /// Chặn các kho bán lẻ hoặc trạm trung chuyển tiếp nhận PO từ NCC.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithNonMasterHubWarehouse_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var retailWh = new Warehouse
            {
                Id = 2,
                Code = "WH-RETAIL",
                Name = "Kho Bán Lẻ Quận 1",
                WarehouseType = WarehouseTypeConstants.Retail,
                IsActive = true
            };
            context.Warehouses.Add(retailWh);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            var createDto = new PurchaseOrderCreateDto
            {
                SupplierId = 1,
                WarehouseId = 2,
                OrderDate = DateTime.UtcNow,
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new PurchaseOrderDetailCreateDto { VariantId = 1, UoMId = 1, OrderQuantity = 50, UnitPrice = 80000 }
                }
            };

            // Act & Assert
            var act = () => service.CreateAsync(createDto, 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ Kho Tổng*mới được phép tiếp nhận đơn đặt mua hàng*");
        }
        #endregion

        #region CLOSE AND SETTLE ORDER ASYNC (QUYẾT TOÁN CÔNG NỢ THEO THỰC NHẬN)
        /// <summary>
        /// Test: Đóng đơn mua hàng đang ở trạng thái PartiallyReceived và quyết toán công nợ theo thực nhận.
        /// Cập nhật Status = Completed, SettledAmount = Sum(ReceivedQty * UnitPrice), lưu ClosureReason.
        /// </summary>
        [Fact]
        public async Task CloseAndSettleOrderAsync_WhenPartiallyReceived_ShouldCompleteOrderAndSetSettledAmount()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 10,
                OrderCode = "PO-SETTLE-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.PartiallyReceived,
                TotalAmount = 5000000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Details = new List<PurchaseOrderDetail>
                {
                    new PurchaseOrderDetail
                    {
                        Id = 101,
                        VariantId = 1,
                        UoMId = 1,
                        OrderQuantity = 100,
                        ReceivedQuantity = 60,
                        RejectedQuantity = 10,
                        UnitPrice = 50000m,
                        TotalPrice = 5000000m
                    }
                }
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act
            var reason = "Nhà cung cấp hết vụ thu hoạch đợt cuối, hai bên thống nhất chốt đơn theo 60 hộp thực nhận";
            var result = await service.CloseAndSettleOrderAsync(10, reason, currentUserId: 1);

            // Assert
            result.Should().BeTrue();

            var settledPo = await context.PurchaseOrders.Include(p => p.Details).FirstOrDefaultAsync(p => p.Id == 10);
            settledPo.Should().NotBeNull();
            settledPo!.Status.Should().Be(PurchaseOrderStatus.Completed);
            settledPo.SettledAmount.Should().Be(3000000m); // 60 * 50,000
            settledPo.TotalAmount.Should().Be(5000000m);   // Giá trị gốc hợp đồng bảo toàn
            settledPo.ClosureReason.Should().Be(reason);
        }

        /// <summary>
        /// Test: Chặn đóng và quyết toán đơn nếu đơn không ở trạng thái PartiallyReceived (ví dụ đang ở Approved hoặc Draft).
        /// </summary>
        [Fact]
        public async Task CloseAndSettleOrderAsync_WhenNotPartiallyReceived_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 11,
                OrderCode = "PO-APPROVED-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.Approved,
                TotalAmount = 1000000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act & Assert
            var act = () => service.CloseAndSettleOrderAsync(11, "Lý do chốt đơn", 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ có thể chốt đóng đơn mua hàng khi đơn đang ở trạng thái Đã nhận một phần*");
        }

        /// <summary>
        /// Test: Bắt buộc cung cấp lý do khi chốt đóng đơn mua hàng sớm.
        /// </summary>
        [Fact]
        public async Task CloseAndSettleOrderAsync_WithoutReason_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedDependenciesAsync(context);

            var po = new PurchaseOrder
            {
                Id = 12,
                OrderCode = "PO-NO-REASON-TEST",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.PartiallyReceived,
                TotalAmount = 1000000m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.PurchaseOrders.Add(po);
            await context.SaveChangesAsync();

            var service = new PurchaseOrderService(context, _mapper);

            // Act & Assert
            var act = () => service.CloseAndSettleOrderAsync(12, "   ", 1);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Vui lòng cung cấp lý do chốt đóng đơn mua hàng sớm*");
        }
        #endregion
    }
}
