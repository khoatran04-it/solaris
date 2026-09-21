using AutoMapper;
using backend.Data;
using backend.DTOs.InventoryReceiptDTOs;
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
    /// INTEGRATION & END-TO-END TEST SUITE: InboundRejectionAndDebtSettlementFlowTests
    /// ============================================================================
    /// Kiểm thử toàn diện và liên thông chu trình:
    /// 1. Tiếp nhận hàng tại Dock từ Nhà cung cấp: Phân loại hàng nhận vs hàng từ chối (trả hàng không nhận)
    /// 2. Hàng từ chối tại dock KHÔNG ghi nhận vào QuantityDamaged, KHÔNG chiếm CBM kho
    /// 3. Miễn trừ bắt buộc chọn Lô hàng (BatchId) khi một mặt hàng bị từ chối 100%
    /// 4. Lưu vết lũy kế số lượng từ chối RejectedQuantity lên PurchaseOrderDetail
    /// 5. Chốt đóng đơn mua hàng sớm (CloseAndSettlePO) khi đơn ở trạng thái PartiallyReceived
    /// 6. Quyết toán công nợ theo thực nhận (SettledAmount) và giải phóng cam kết tiền hàng (PendingCommitment = 0)
    /// 7. Đối chiếu dữ liệu tài chính công nợ NCC trên Dashboard Quản trị (SupplierPayables & ShrinkageLoss)
    /// 8. Phân định rõ ràng giữa Hàng từ chối NCC (không gây tổn thất kho) vs Hàng thu hồi RMA từ khách (tạo QuantityDamaged)
    /// 9. Khiên an toàn (Safety Shields) bảo vệ tính toàn vẹn trạng thái đơn và kiểm định lô hàng
    /// </summary>
    public class InboundRejectionAndDebtSettlementFlowTests
    {
        private readonly IMapper _mapper;

        public InboundRejectionAndDebtSettlementFlowTests()
        {
            _mapper = TestFactories.CreateAutoMapper();
        }

        #region Helper Setup
        private static async Task SeedMasterDataAsync(SolarisDbContext context)
        {
            // 1. Kho Tổng MasterHub
            context.Warehouses.Add(new Warehouse
            {
                Id = 1,
                Code = "WH-MASTER",
                Name = "Tổng Kho Solaris Củ Chi",
                WarehouseType = WarehouseTypeConstants.MasterHub,
                TotalCapacityCbm = 1000m,
                WarningThresholdPercent = 80,
                IsActive = true
            });

            // 2. Nhà cung cấp
            context.Suppliers.Add(new Supplier
            {
                Id = 1,
                Code = "SUP-DALAT",
                Name = "Hợp Tác Xã Rau Củ Đà Lạt",
                Phone = "02633888999",
                Email = "dalatgap@solaris.vn",
                TaxCode = "5801234567",
                IsActive = true
            });

            // 3. Đơn vị tính
            context.UoMs.Add(new UoM { Id = 1, Code = "KG", Name = "Kilogram", IsActive = true });

            // 4. Danh mục & Sản phẩm & Biến thể
            context.ProductCategories.Add(new ProductCategory { Id = 1, Code = "CAT-RAU", Name = "Rau Củ Tươi", IsActive = true });
            context.Products.Add(new Product
            {
                Id = 1,
                Code = "PROD-BAPCAI",
                Name = "Bắp Cải Trắng Đà Lạt VietGAP",
                CategoryId = 1,
                BaseUoMId = 1,
                IsActive = true
            });
            context.ProductVariants.Add(new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Code = "SKU-BAPCAI-1KG",
                Name = "Bắp Cải Trắng Bịch 1Kg",
                GrossWeightKg = 1m,
                UnitCbm = 0.002m,
                IsActive = true
            });

            // 5. Lô hàng cho hàng đạt chuẩn
            context.ProductBatches.Add(new ProductBatch
            {
                Id = 1,
                BatchCode = "BATCH-BC-20260920",
                VariantId = 1,
                SupplierId = 1,
                ManufactureDate = DateTime.UtcNow.AddDays(-1),
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsActive = true
            });

            // 6. Tài khoản quản trị kho
            context.IAUsers.Add(new IAUser
            {
                Id = 1,
                CitizenId = "079090123456",
                Username = "admin_kho",
                FullName = "Trần Quản Kho",
                PhoneNumber = "0988776655",
                Email = "kho@solaris.vn",
                PasswordHash = "hash",
                IsActive = true
            });

            await context.SaveChangesAsync();
        }
        #endregion

        #region TEST 1: CHU TRÌNH LIÊN THÔNG ĐẦY ĐỦ (END-TO-END FLOW)
        /// <summary>
        /// Kịch bản kiểm thử toàn diện từ lúc lập PO -> Giao đợt 1 có từ chối -> Giao đợt 2 từ chối 100% không cần lô ->
        /// Chốt đóng đơn theo thực nhận -> Đối soát công nợ NCC trên Dashboard.
        /// </summary>
        [Fact]
        public async Task EndToEnd_InboundRejection_And_DebtSettlement_Flow()
        {
            // ========================================================================
            // BƯỚC 1: KHỞI TẠO HỆ THỐNG VÀ LẬP ĐƠN MUA HÀNG (PO)
            // ========================================================================
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedMasterDataAsync(context);

            var poService = new PurchaseOrderService(context, _mapper);
            var receiptService = new InventoryReceiptService(context, _mapper);
            var dashboardService = new DashboardService(context);

            // Tạo PO: Đặt 100 kg Bắp Cải với đơn giá 50,000 đ/kg = 5,000,000 đ
            var createPoDto = new PurchaseOrderCreateDto
            {
                OrderCode = "PO-2026-BAPCAI-01",
                SupplierId = 1,
                WarehouseId = 1,
                OrderDate = DateTime.UtcNow,
                ExpectedDeliveryDate = DateTime.UtcNow.AddDays(2),
                Note = "Giao hàng bắp cải tươi chuẩn VietGAP",
                Details = new List<PurchaseOrderDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        UoMId = 1,
                        OrderQuantity = 100,
                        UnitPrice = 50000m
                    }
                }
            };

            var poId = await poService.CreateAsync(createPoDto, currentUserId: 1);
            await poService.UpdateStatusAsync(poId, new PurchaseOrderStatusUpdateDto { Status = PurchaseOrderStatus.Processing });
            await poService.UpdateStatusAsync(poId, new PurchaseOrderStatusUpdateDto { Status = PurchaseOrderStatus.Approved });

            var poAfterApproval = await context.PurchaseOrders.Include(p => p.Details).FirstAsync(p => p.Id == poId);
            poAfterApproval.Status.Should().Be(PurchaseOrderStatus.Approved);
            poAfterApproval.TotalAmount.Should().Be(5000000m);
            var poDetailId = poAfterApproval.Details.First().Id;

            // ========================================================================
            // BƯỚC 2: GIAO ĐỢT 1 - XE TẢI TỚI DOCK (70 KG: 60 KG ĐẠT, 10 KG BỊ TỪ CHỐI)
            // ========================================================================
            var receipt1CreateDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Note = "Nhận hàng đợt 1 từ xe tải lạnh 49C-123.45",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1, // Lô hàng lưu kho
                        UoMId = 1,
                        PurchaseOrderDetailId = poDetailId,
                        ExpectedQuantity = 100,
                        AcceptedQuantity = 60, // Thực nhận 60 kg
                        RejectedQuantity = 10, // Từ chối 10 kg ngay tại sàn dock (trả lại xe)
                        RejectReason = "10 kg bị dập nát và sâu đục khi QC kiểm tra"
                    }
                }
            };

            var receipt1Id = await receiptService.CreateAsync(receipt1CreateDto);
            var complete1Success = await receiptService.CompleteReceiptAsync(receipt1Id, receivedById: 1, note: "Đã bốc dỡ 60 kg vào kho, 10 kg trả lại xe");
            complete1Success.Should().BeTrue();

            // KIỂM TRA TỒN KHO SAU ĐỢT 1:
            var invAfterReceipt1 = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1 && i.BatchId == 1);
            invAfterReceipt1.QuantityAvailable.Should().Be(60, "Chỉ 60 kg đạt chuẩn mới được nhập kho");
            invAfterReceipt1.QuantityDamaged.Should().Be(0, "10 kg bị từ chối tại dock KHÔNG ĐƯỢC tạo tồn kho hỏng ảo!");

            // KIỂM TRA TRẠNG THÁI PO SAU ĐỢT 1:
            var poAfterReceipt1 = await context.PurchaseOrders.Include(p => p.Details).FirstAsync(p => p.Id == poId);
            poAfterReceipt1.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
            var detailAfterReceipt1 = poAfterReceipt1.Details.First();
            detailAfterReceipt1.ReceivedQuantity.Should().Be(60);
            detailAfterReceipt1.RejectedQuantity.Should().Be(10, "Lũy kế từ chối đợt 1 phải là 10");

            // ========================================================================
            // BƯỚC 3: GIAO ĐỢT 2 - GIAO 30 KG CÒN LẠI NHƯNG BỊ TỪ CHỐI 100% (KHÔNG CẦN CHỌN LÔ)
            // ========================================================================
            var receipt2CreateDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Note = "Nhận hàng đợt 2 - xe giao 30 kg còn lại",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = null, // MIỄN TRỪ LÔ KHI TỪ CHỐI 100%
                        UoMId = 1,
                        PurchaseOrderDetailId = poDetailId,
                        ExpectedQuantity = 30,
                        AcceptedQuantity = 0,  // Không nhận kg nào
                        RejectedQuantity = 30, // Trả lại toàn bộ 30 kg
                        RejectReason = "Xe mất lạnh, toàn bộ 30 kg bị héo úa không đạt tiêu chuẩn"
                    }
                }
            };

            // Tạo phiếu thành công dù BatchId = null
            var receipt2Id = await receiptService.CreateAsync(receipt2CreateDto);
            receipt2Id.Should().BeGreaterThan(0);

            var complete2Success = await receiptService.CompleteReceiptAsync(receipt2Id, receivedById: 1, note: "Từ chối 100% tại dock");
            complete2Success.Should().BeTrue();

            // KIỂM TRA TỒN KHO SAU ĐỢT 2:
            var allInventories = await context.WarehouseInventories.Where(i => i.WarehouseId == 1 && i.VariantId == 1).ToListAsync();
            allInventories.Sum(i => i.QuantityAvailable).Should().Be(60, "Tồn kho khả dụng vẫn là 60");
            allInventories.Sum(i => i.QuantityDamaged).Should().Be(0, "Vẫn không có tồn kho hỏng nào trong kho");

            // KIỂM TRA TIẾN ĐỘ PO SAU ĐỢT 2:
            var poAfterReceipt2 = await context.PurchaseOrders.Include(p => p.Details).FirstAsync(p => p.Id == poId);
            poAfterReceipt2.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
            var detailAfterReceipt2 = poAfterReceipt2.Details.First();
            detailAfterReceipt2.ReceivedQuantity.Should().Be(60);
            detailAfterReceipt2.RejectedQuantity.Should().Be(40, "Tổng lũy kế từ chối qua 2 đợt phải là 10 + 30 = 40 kg");

            // ========================================================================
            // BƯỚC 4: NCC HẾT HÀNG -> CHỐT ĐÓNG ĐƠN MUA HÀNG (CLOSE AND SETTLE PO)
            // ========================================================================
            var closureReason = "NCC thông báo hết mùa vụ bắp cải, hai bên thỏa thuận chốt đơn theo 60 kg thực nhận";
            var closeSuccess = await poService.CloseAndSettleOrderAsync(poId, closureReason, currentUserId: 1);
            closeSuccess.Should().BeTrue();

            var poFinal = await context.PurchaseOrders.Include(p => p.Details).FirstAsync(p => p.Id == poId);
            poFinal.Status.Should().Be(PurchaseOrderStatus.Completed, "Đơn phải chuyển sang Completed");
            poFinal.SettledAmount.Should().Be(3000000m, "Quyết toán công nợ chốt theo đúng 60 kg * 50,000 đ = 3,000,000 đ");
            poFinal.TotalAmount.Should().Be(5000000m, "Hợp đồng gốc 5,000,000 đ vẫn được bảo toàn nguyên vẹn");
            poFinal.ClosureReason.Should().Be(closureReason);

            // ========================================================================
            // BƯỚC 5: ĐỐI CHIẾU DỮ LIỆU TÀI CHÍNH & CÔNG NỢ TRÊN DASHBOARD QUẢN TRỊ
            // ========================================================================
            var financeDashboard = await dashboardService.GetFinancialPerformanceAsync("30days", null, null);
            financeDashboard.Should().NotBeNull();

            var supplierPayable = financeDashboard.SupplierPayables.FirstOrDefault(s => s.SupplierId == 1);
            supplierPayable.Should().NotBeNull();
            supplierPayable!.TotalPoValue.Should().Be(5000000m, "Tổng giá trị hợp đồng PO là 5,000,000 đ");
            supplierPayable.ReceivedValue.Should().Be(3000000m, "Công nợ phải trả NCC đúng bằng 3,000,000 đ theo giá trị thực nhận đã chốt");
            supplierPayable.QcRejectedValue.Should().Be(2000000m, "Giá trị hàng bị QC từ chối là 40 kg * 50,000 đ = 2,000,000 đ");
            supplierPayable.PendingCommitment.Should().Be(0m, "Đơn đã hoàn tất chốt sổ nên không còn đồng cam kết hàng chờ nào treo");

            // Kiểm tra tổn thất hao hụt kho hàng (Shrinkage Loss)
            financeDashboard.ShrinkageLoss.DamagedStockValue.Should().Be(0m, "Kho không phải chịu tổn thất hao hụt vì hàng hỏng đã bị trả lại tại dock!");
        }
        #endregion

        #region TEST 2: PHÂN BIỆT HÀNG TỪ CHỐI NCC VS HÀNG THU HỒI KHÁCH HÀNG (RMA)
        /// <summary>
        /// Kiểm chứng sự khác biệt rạch ròi:
        /// - NCC bị từ chối tại dock: Hàng trên xe NCC -> KHÔNG cộng vào QuantityDamaged.
        /// - RMA thu hồi từ khách hàng: Hàng nhận về kho cách ly -> CÓ cộng vào QuantityDamaged.
        /// </summary>
        [Fact]
        public async Task Contrast_CustomerRmaReturn_RoutesToQuantityDamaged_WhileSupplierRejectionDoesNot()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedMasterDataAsync(context);

            var receiptService = new InventoryReceiptService(context, _mapper);
            var dashboardService = new DashboardService(context);

            // 1. Phiếu nhập từ NCC có 8 đơn vị bị từ chối
            var supplierReceiptDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Note = "Nhập hàng từ NCC HTX Đà Lạt",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ExpectedQuantity = 50,
                        AcceptedQuantity = 42,
                        RejectedQuantity = 8,
                        RejectReason = "8 kg dập vỏ ngoài xe tải"
                    }
                }
            };
            var supReceiptId = await receiptService.CreateAsync(supplierReceiptDto);
            await receiptService.CompleteReceiptAsync(supReceiptId, 1, "Hoàn tất kiểm đếm NCC");

            // Assert: Chưa có hàng hỏng nào trong kho
            var invAfterSup = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            invAfterSup.QuantityAvailable.Should().Be(42);
            invAfterSup.QuantityDamaged.Should().Be(0);

            // 2. Phiếu nhập kho thu hồi khách hàng RMA có 5 đơn vị dập hỏng
            var rmaReceiptDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = null, // RMA không có SupplierId
                Note = "Thu hồi RMA từ đơn khách hàng RET-20260920-001",
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = 1,
                        UoMId = 1,
                        ExpectedQuantity = 10,
                        AcceptedQuantity = 5,
                        RejectedQuantity = 5,
                        RejectReason = "5 kg dập nát do shipper làm rơi"
                    }
                }
            };
            var rmaReceiptId = await receiptService.CreateAsync(rmaReceiptDto);
            await receiptService.CompleteReceiptAsync(rmaReceiptId, 1, "Hoàn tất nhập kho RMA");

            // Assert: Hàng hỏng từ khách được đưa vào khu cách ly kho
            var invAfterRma = await context.WarehouseInventories.FirstAsync(i => i.WarehouseId == 1 && i.VariantId == 1);
            invAfterRma.QuantityAvailable.Should().Be(47); // 42 + 5
            invAfterRma.QuantityDamaged.Should().Be(5);   // Đúng 5 đơn vị hàng RMA hỏng

            // Kiểm tra giá trị tồn kho hỏng trên Dashboard
            var dashboard = await dashboardService.GetFinancialPerformanceAsync("30days", null, null);
            dashboard.ShrinkageLoss.DamagedStockValue.Should().Be(5 * 40000m, "Chỉ tính 5 đơn vị RMA hỏng theo giá vốn tồn kho định mức (5 * 40,000 = 200,000 đ), hoàn toàn không tính 8 đơn vị NCC bị từ chối tại dock");
        }
        #endregion

        #region TEST 3: VALIDATION CHỌN LÔ HÀNG (BATCH REQUIREMENT SHIELDS)
        /// <summary>
        /// Kiểm chứng:
        /// - Khi AcceptedQuantity > 0, BẮT BUỘC phải có BatchId để quản lý FEFO.
        /// - Khi AcceptedQuantity == 0 (từ chối 100%), BatchId ĐƯỢC PHÉP null.
        /// </summary>
        [Fact]
        public async Task Validation_ReceiptDetailBatchRequirement_EnforcedOnlyWhenAcceptedQtyGreaterThanZero()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedMasterDataAsync(context);

            var receiptService = new InventoryReceiptService(context, _mapper);

            // TH 1: Nhận 10 kg nhưng không chọn lô -> Bị chặn
            var invalidDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = null, // Thiếu lô khi nhận hàng
                        UoMId = 1,
                        ExpectedQuantity = 10,
                        AcceptedQuantity = 10,
                        RejectedQuantity = 0
                    }
                }
            };

            var actInvalid = () => receiptService.CreateAsync(invalidDto);
            await actInvalid.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*bắt buộc phải được gắn vào một Lô hàng*");

            // TH 2: Nhận 0 kg, từ chối 10 kg, không chọn lô -> Hợp lệ và thành công
            var valid100PercentRejectDto = new InventoryReceiptCreateDto
            {
                WarehouseId = 1,
                SupplierId = 1,
                Details = new List<InventoryReceiptDetailCreateDto>
                {
                    new()
                    {
                        VariantId = 1,
                        BatchId = null, // Được phép null
                        UoMId = 1,
                        ExpectedQuantity = 10,
                        AcceptedQuantity = 0,
                        RejectedQuantity = 10,
                        RejectReason = "Hàng ẩm mốc trả lại NCC ngay"
                    }
                }
            };

            var receiptId = await receiptService.CreateAsync(valid100PercentRejectDto);
            receiptId.Should().BeGreaterThan(0);
        }
        #endregion

        #region TEST 4: KHIÊN AN TOÀN CHỐT ĐÓNG ĐƠN MUA HÀNG (SAFETY SHIELDS)
        /// <summary>
        /// Kiểm chứng các điều kiện bảo vệ khi chốt đóng đơn PO:
        /// - Chỉ cho phép khi đơn ở trạng thái PartiallyReceived.
        /// - Chặn khi đơn ở Draft, Approved, hay Completed.
        /// - Bắt buộc nhập lý do đóng đơn.
        /// </summary>
        [Fact]
        public async Task SafetyShields_CloseAndSettleOrder_GuardsCorrectly()
        {
            // Arrange
            using var context = TestFactories.CreateInMemoryDbContext();
            await SeedMasterDataAsync(context);

            var poService = new PurchaseOrderService(context, _mapper);

            // Đơn 1: Đang ở trạng thái Approved (chưa nhập đợt nào)
            context.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = 101,
                OrderCode = "PO-APPROVED-NO-RECEIPT",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.Approved,
                TotalAmount = 2000000m
            });

            // Đơn 2: Đang ở trạng thái Draft
            context.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = 102,
                OrderCode = "PO-DRAFT",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.Draft,
                TotalAmount = 1000000m
            });

            // Đơn 3: Đang ở PartiallyReceived
            context.PurchaseOrders.Add(new PurchaseOrder
            {
                Id = 103,
                OrderCode = "PO-PARTIAL",
                WarehouseId = 1,
                SupplierId = 1,
                CreatedById = 1,
                Status = PurchaseOrderStatus.PartiallyReceived,
                TotalAmount = 3000000m,
                Details = new List<PurchaseOrderDetail>
                {
                    new() { Id = 501, VariantId = 1, UoMId = 1, OrderQuantity = 60, ReceivedQuantity = 20, UnitPrice = 50000m }
                }
            });

            await context.SaveChangesAsync();

            // Act & Assert 1: Chặn chốt đơn Approved
            var actApproved = () => poService.CloseAndSettleOrderAsync(101, "Lý do", 1);
            await actApproved.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ có thể chốt đóng đơn mua hàng khi đơn đang ở trạng thái Đã nhận một phần*");

            // Act & Assert 2: Chặn chốt đơn Draft
            var actDraft = () => poService.CloseAndSettleOrderAsync(102, "Lý do", 1);
            await actDraft.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Chỉ có thể chốt đóng đơn mua hàng khi đơn đang ở trạng thái Đã nhận một phần*");

            // Act & Assert 3: Chặn chốt đơn khi không nhập lý do
            var actNoReason = () => poService.CloseAndSettleOrderAsync(103, "   ", 1);
            await actNoReason.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Vui lòng cung cấp lý do chốt đóng đơn mua hàng sớm*");

            // Act & Assert 4: Chốt thành công khi đủ điều kiện
            var result = await poService.CloseAndSettleOrderAsync(103, "NCC hết hàng giao đợt 2", 1);
            result.Should().BeTrue();

            var closedPo = await context.PurchaseOrders.FindAsync(103);
            closedPo!.Status.Should().Be(PurchaseOrderStatus.Completed);
            closedPo.SettledAmount.Should().Be(20 * 50000m);
        }
        #endregion
    }
}
