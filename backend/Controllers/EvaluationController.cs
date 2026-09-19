using backend.Data;
using backend.Helpers;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Controllers
{
    /// <summary>
    /// API Phục Vụ Thực Nghiệm Định Lượng Chương 4 (Thesis Chapter 4 Evaluation).
    /// Cung cấp các endpoint: Nạp dữ liệu giả Bogus, Đo lường kiểm thử tương tranh (Stress Test),
    /// Kiểm tra phân bổ lô FEFO, và Reset trạng thái kho cho các kịch bản đo kiểm.
    /// </summary>
    [ApiController]
    [Route("api/evaluation")]
    [AllowAnonymous]
    [Produces("application/json")]
    public class EvaluationController : ControllerBase
    {
        private readonly SolarisDbContext _context;
        private readonly IOrderRoutingService _routingService;
        private readonly IDistanceService _distanceService;

        public EvaluationController(
            SolarisDbContext context,
            IOrderRoutingService routingService,
            IDistanceService distanceService)
        {
            _context = context;
            _routingService = routingService;
            _distanceService = distanceService;
        }

        /// <summary>
        /// 1. Kích hoạt nạp dữ liệu mầm Bogus vào cơ sở dữ liệu local (5 Kho, 10 Sản phẩm, Lô FEFO, 20 Khách hàng).
        /// </summary>
        [HttpPost("seed")]
        public async Task<ActionResult<SeedingSummaryDto>> SeedEvaluationData()
        {
            try
            {
                var summary = await BogusDataSeeder.SeedEvaluationDataAsync(_context);
                return Ok(new
                {
                    message = "Nạp dữ liệu thực nghiệm Bogus thành công!",
                    data = summary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Lỗi khi nạp dữ liệu mầm",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// 1b. Nạp dữ liệu mở rộng (Option A: ~50 Sản phẩm nông sản đặc sản, biến thể, tồn kho, 100 khách hàng, 150 đơn hàng lịch sử).
        /// </summary>
        [HttpPost("seed-extended")]
        public async Task<ActionResult<ExtendedSeedingSummaryDto>> SeedExtendedData()
        {
            try
            {
                var summary = await ExtendedCatalogSeeder.SeedAsync(_context);
                return Ok(new
                {
                    message = "Nạp dữ liệu nông sản mở rộng thành công!",
                    data = summary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Lỗi khi nạp dữ liệu mở rộng",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }


        /// <summary>
        /// 2. Báo cáo trạng thái dữ liệu phục vụ thực nghiệm (Metrics Status).
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var stressCode = "SKU-STRESS-50";
            var stressInventory = await _context.WarehouseInventories
                .Include(wi => wi.Variant)
                .Include(wi => wi.Warehouse)
                .Include(wi => wi.Batch)
                .Where(wi => wi.Variant != null && wi.Variant.Code == stressCode)
                .Select(wi => new
                {
                    Warehouse = wi.Warehouse!.Name,
                    Variant = wi.Variant!.Name,
                    VariantCode = wi.Variant.Code,
                    Batch = wi.Batch != null ? wi.Batch.BatchCode : "N/A",
                    QuantityAvailable = wi.QuantityAvailable,
                    QuantityReserved = wi.QuantityReserved,
                    UpdatedAt = wi.UpdatedAt
                })
                .FirstOrDefaultAsync();

            var totalReserveTx = await _context.InventoryTransactions
                .CountAsync(t => t.Type == TransactionType.Reserve && t.ReferenceCode.StartsWith("ORD-STRESS"));

            return Ok(new
            {
                TotalWarehouses = await _context.Warehouses.CountAsync(w => !w.IsDeleted),
                TotalCategories = await _context.ProductCategories.CountAsync(c => !c.IsDeleted),
                TotalProducts = await _context.Products.CountAsync(p => !p.IsDeleted),
                TotalVariants = await _context.ProductVariants.CountAsync(v => !v.IsDeleted),
                TotalBatches = await _context.ProductBatches.CountAsync(b => !b.IsDeleted),
                TotalInventories = await _context.WarehouseInventories.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(c => !c.IsDeleted),
                TotalAddresses = await _context.CustomerAddresses.CountAsync(a => !a.IsDeleted),
                StressTestSKU = stressInventory,
                TotalStressReservationsRecorded = totalReserveTx
            });
        }

        /// <summary>
        /// 3. Reset tồn kho SKU-STRESS-50 về chính xác 50.000 để chạy lại Stress Test JMeter.
        /// </summary>
        [HttpPost("reset-stress-inventory")]
        public async Task<IActionResult> ResetStressInventory()
        {
            var stressVariant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Code == "SKU-STRESS-50");
            if (stressVariant == null)
                return NotFound(new { message = "Chưa khởi tạo SKU-STRESS-50. Vui lòng gọi /api/evaluation/seed trước." });

            var now = DateTime.UtcNow;
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE WarehouseInventories SET QuantityAvailable = 50.000, QuantityReserved = 0.000, UpdatedAt = {now} WHERE VariantId = {stressVariant.Id}");

            // Xóa các giao dịch stress test cũ để làm sạch số liệu đo
            var oldStressTxs = await _context.InventoryTransactions
                .Where(t => t.ReferenceCode.StartsWith("ORD-STRESS"))
                .ToListAsync();
            _context.InventoryTransactions.RemoveRange(oldStressTxs);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã reset tồn kho SKU-STRESS-50 về chính xác 50 sản phẩm khả dụng. Đã xóa lịch sử giao dịch stress cũ.",
                quantityAvailable = 50.000,
                quantityReserved = 0.000
            });
        }

        /// <summary>
        /// Reset tồn kho Cải Bó Xôi về 30kg (cận hạn) và 70kg (hạn xa) để chụp màn hình Before / After FEFO.
        /// </summary>
        [HttpPost("reset-fefo-inventory")]
        public async Task<IActionResult> ResetFefoInventory()
        {
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Code == "PRD-CAI-BO-XOI-SKU1");
            if (variant == null) return NotFound(new { message = "Không tìm thấy biến thể PRD-CAI-BO-XOI-SKU1" });

            var now = DateTime.UtcNow;
            var batchNear = await _context.ProductBatches.FirstOrDefaultAsync(b => b.BatchCode == $"LOT-{variant.Id}-03D");
            var batchFar = await _context.ProductBatches.FirstOrDefaultAsync(b => b.BatchCode == $"LOT-{variant.Id}-15D");

            if (batchNear != null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE WarehouseInventories SET QuantityAvailable = 30.000, QuantityReserved = 0.000, UpdatedAt = {now} WHERE VariantId = {variant.Id} AND BatchId = {batchNear.Id}");
            }
            if (batchFar != null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE WarehouseInventories SET QuantityAvailable = 70.000, QuantityReserved = 0.000, UpdatedAt = {now} WHERE VariantId = {variant.Id} AND BatchId = {batchFar.Id}");
            }

            return Ok(new { message = "Đã reset lô Cải Bó Xôi về 30kg (cận hạn) và 70kg (hạn xa) tại tất cả các kho." });
        }

        /// <summary>
        /// 4. Endpoint Giữ chỗ Tồn kho đồng thời (Concurrency Stress Test Endpoint cho RQ2).
        /// Mô phỏng khách hàng gửi yêu cầu checkout 1 đơn vị sản phẩm SKU-STRESS-50.
        /// Áp dụng Row-level Atomic Conditional Update và Transaction Isolation để chống Overselling.
        /// </summary>
        [HttpPost("stress-reserve")]
        public async Task<IActionResult> StressReserve([FromQuery] int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            try
            {
                var result = await _context.ExecuteInTransactionAsync(async () =>
                {
                    var now = DateTime.UtcNow;

                    // 1. Tìm tồn kho khả dụng còn đủ số lượng của SKU-STRESS-50
                    var inventory = await _context.WarehouseInventories
                        .Include(wi => wi.Variant)
                        .Include(wi => wi.Batch)
                        .Where(wi => wi.Variant != null && wi.Variant.Code == "SKU-STRESS-50" && wi.QuantityAvailable >= quantity)
                        .OrderBy(wi => wi.Batch != null ? wi.Batch.ExpiryDate : DateTime.MaxValue)
                        .FirstOrDefaultAsync();

                    if (inventory == null)
                    {
                        throw new InvalidOperationException("Hết hàng tồn kho khả dụng (Out of stock).");
                    }

                    // 2. Kỹ thuật Cập nhật Nguyên tử ở mức hàng (Row-level Atomic Conditional Update)
                    // Chỉ trừ kho thành công nếu tại thời điểm khóa hàng thực tế QuantityAvailable >= quantity
                    int rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE WarehouseInventories SET QuantityAvailable = QuantityAvailable - {quantity}, QuantityReserved = QuantityReserved + {quantity}, UpdatedAt = {now} WHERE Id = {inventory.Id} AND QuantityAvailable >= {quantity}");

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException("Xung đột tương tranh: Lô hàng vừa hết hoặc đã được giữ chỗ bởi giao dịch khác.");
                    }

                    // 3. Ghi sổ cái bất biến Reserve (Audit Ledger)
                    string orderRef = $"ORD-STRESS-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant();
                    var tx = new InventoryTransaction
                    {
                        TransactionCode = $"TX-STRESS-{Guid.NewGuid():N}".Substring(0, 20).ToUpperInvariant(),
                        Type = TransactionType.Reserve,
                        WarehouseId = inventory.WarehouseId,
                        VariantId = inventory.VariantId,
                        BatchId = inventory.BatchId,
                        Quantity = quantity,
                        ReferenceCode = orderRef,
                        Note = "Giao dịch giữ chỗ kiểm thử tương tranh RQ2",
                        CreatedById = 1, // System User
                        CreatedAt = now
                    };
                    _context.InventoryTransactions.Add(tx);
                    await _context.SaveChangesAsync();

                    // Lấy số dư tức thời sau khi cập nhật
                    var currentStock = await _context.WarehouseInventories
                        .Where(wi => wi.Id == inventory.Id)
                        .Select(wi => new { wi.QuantityAvailable, wi.QuantityReserved })
                        .FirstAsync();

                    return new
                    {
                        success = true,
                        orderReference = orderRef,
                        transactionCode = tx.TransactionCode,
                        quantityReserved = quantity,
                        remainingStock = currentStock.QuantityAvailable,
                        totalReserved = currentStock.QuantityReserved
                    };
                });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "OUT_OF_STOCK_OR_CONCURRENCY_CONFLICT",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    error = "TRANSACTION_ERROR",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// 5. Endpoint Kiểm tra Thuật toán FEFO (Bóc tách Đa lô theo Hạn dùng).
        /// Cho phép xuất số lượng lớn (ví dụ 40kg) để quan sát hệ thống vét cạn lô cận hạn trước rồi gối đầu lô hạn xa.
        /// </summary>
        [HttpPost("test-fefo-allocation")]
        public async Task<IActionResult> TestFefoAllocation([FromQuery] string variantCode = "PRD-CAI-BO-XOI-SKU1", [FromQuery] decimal quantity = 40)
        {
            var now = DateTime.UtcNow;
            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Code == variantCode);
            if (variant == null)
                return NotFound(new { message = $"Không tìm thấy biến thể {variantCode}" });

            var primaryWh = await _context.Warehouses.FirstOrDefaultAsync(w => w.Code == "WH-BT-01" && !w.IsDeleted);
            if (primaryWh == null)
                return NotFound(new { message = "Không tìm thấy kho Bình Thạnh" });

            // Lấy danh sách lô theo FEFO (ExpiryDate ASC)
            var inventories = await _context.WarehouseInventories
                .Include(wi => wi.Batch)
                .Where(wi => wi.WarehouseId == primaryWh.Id && wi.VariantId == variant.Id && wi.QuantityAvailable > 0 && wi.Batch != null && wi.Batch.ExpiryDate > now)
                .OrderBy(wi => wi.Batch!.ExpiryDate)
                .ToListAsync();

            var beforeState = inventories.Select(i => new
            {
                BatchCode = i.Batch!.BatchCode,
                ExpiryDate = i.Batch.ExpiryDate.ToString("yyyy-MM-dd"),
                DaysRemaining = (i.Batch.ExpiryDate - now).TotalDays,
                QuantityAvailable = i.QuantityAvailable,
                QuantityReserved = i.QuantityReserved
            }).ToList();

            decimal remainingNeeded = quantity;
            var allocatedBatches = new List<object>();

            // Thực hiện bóc tách FEFO
            await _context.ExecuteInTransactionAsync(async () =>
            {
                foreach (var inv in inventories)
                {
                    if (remainingNeeded <= 0) break;

                    decimal pickQty = Math.Min(inv.QuantityAvailable, remainingNeeded);

                    int rows = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE WarehouseInventories SET QuantityAvailable = QuantityAvailable - {pickQty}, QuantityReserved = QuantityReserved + {pickQty}, UpdatedAt = {now} WHERE Id = {inv.Id} AND QuantityAvailable >= {pickQty}");

                    if (rows > 0)
                    {
                        allocatedBatches.Add(new
                        {
                            BatchCode = inv.Batch!.BatchCode,
                            ExpiryDate = inv.Batch.ExpiryDate.ToString("yyyy-MM-dd"),
                            AllocatedQuantity = pickQty,
                            Strategy = inv.Batch.ExpiryDate < now.AddDays(5) ? "Vét cạn lô cận hạn (3 ngày)" : "Gối đầu lô hạn xa (15 ngày)"
                        });
                        remainingNeeded -= pickQty;
                    }
                }
                await _context.SaveChangesAsync();
            });

            // Lấy lại trạng thái sau khi phân bổ
            var afterInventories = await _context.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Batch)
                .Where(wi => wi.WarehouseId == primaryWh.Id && wi.VariantId == variant.Id)
                .OrderBy(wi => wi.Batch!.ExpiryDate)
                .ToListAsync();

            var afterState = afterInventories.Select(i => new
            {
                BatchCode = i.Batch!.BatchCode,
                ExpiryDate = i.Batch.ExpiryDate.ToString("yyyy-MM-dd"),
                QuantityAvailable = i.QuantityAvailable,
                QuantityReserved = i.QuantityReserved
            }).ToList();

            return Ok(new
            {
                Message = "Thử nghiệm giải thuật FEFO thành công!",
                VariantCode = variantCode,
                Warehouse = primaryWh.Name,
                TotalRequested = quantity,
                Unfulfilled = remainingNeeded,
                Before = beforeState,
                Allocated = allocatedBatches,
                After = afterState
            });
        }

        /// <summary>
        /// 6. Endpoint Đánh giá Định tuyến Chọn Kho & Rào chắn Chuỗi Lạnh 15km (RQ3 Evaluation).
        /// Kiểm thử định lượng thuật toán Haversine, Single-Hub Allocation và Cold-Chain Geofencing.
        /// </summary>
        [HttpPost("test-routing")]
        public async Task<IActionResult> TestRouting([FromBody] RoutingEvaluationRequestDto request)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 1. Lấy thông tin các sản phẩm trong giỏ hàng và kiểm tra yêu cầu chuỗi lạnh
            var variantIds = request.Items.Select(i => i.VariantId).Distinct().ToList();
            var variants = await _context.ProductVariants
                .Include(v => v.Product!)
                    .ThenInclude(p => p.Category!)
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync();

            var itemsInfo = request.Items.Select(item =>
            {
                var v = variants.FirstOrDefault(x => x.Id == item.VariantId);
                bool isCold = v?.Product?.Category?.RequiresColdChain ?? false;
                return new
                {
                    VariantId = item.VariantId,
                    VariantCode = v?.Code ?? "UNKNOWN",
                    VariantName = v?.Name ?? "Sản phẩm không rõ",
                    CategoryName = v?.Product?.Category?.Name ?? "Chưa phân loại",
                    RequiresColdChain = isCold,
                    Quantity = item.Quantity
                };
            }).ToList();

            bool cartRequiresColdChain = itemsInfo.Any(i => i.RequiresColdChain);

            // 2. Lấy danh sách tất cả các kho bán lẻ hợp lệ đang hoạt động
            var retailWarehouses = await _context.Warehouses
                .Include(w => w.Address)
                .Where(w => w.IsActive && !w.IsDeleted && (w.WarehouseType == WarehouseTypeConstants.Retail || string.IsNullOrEmpty(w.WarehouseType)))
                .ToListAsync();

            // 3. Đánh giá chi tiết từng ứng viên kho hàng (Candidate Evaluation)
            var candidateEvaluations = new List<object>();
            Warehouse? selectedWarehouse = null;
            double selectedDistance = double.MaxValue;

            // Sắp xếp các kho theo khoảng cách Haversine từ khách hàng
            var candidatesWithDistance = retailWarehouses.Select(wh =>
            {
                double dist = 5.0; // fallback
                if (wh.Address != null && wh.Address.Latitude != 0 && wh.Address.Longitude != 0 && request.Latitude != 0 && request.Longitude != 0)
                {
                    dist = _distanceService.CalculateDistanceKm(request.Latitude, request.Longitude, wh.Address.Latitude, wh.Address.Longitude);
                }
                else if (wh.Address != null && !string.IsNullOrEmpty(wh.Address.District) && !string.IsNullOrEmpty(request.District))
                {
                    if (GeoHelper.IsSameLocation(wh.Address.District, request.District)) dist = 2.5;
                    else dist = 7.5;
                }
                return new { Warehouse = wh, DistanceKm = dist };
            }).OrderBy(x => x.DistanceKm).ToList();

            foreach (var cand in candidatesWithDistance)
            {
                var wh = cand.Warehouse;
                double dist = cand.DistanceKm;
                double maxColdRadius = wh.MaxColdChainRadiusKm > 0 ? wh.MaxColdChainRadiusKm : 15.0;
                bool withinGeofence = !cartRequiresColdChain || dist <= maxColdRadius;

                // Kiểm tra tồn kho của kho này đối với từng món trong giỏ hàng
                bool hasAllItems = true;
                var stockDetails = new List<object>();

                foreach (var reqItem in request.Items)
                {
                    decimal available = 0;
                    // Nếu kho này bị giả lập thiếu hàng (cho bài test Single-Hub)
                    if (request.ExcludeWarehouseStockId.HasValue && request.ExcludeWarehouseStockId.Value == wh.Id)
                    {
                        available = 0;
                    }
                    else
                    {
                        available = await _context.WarehouseInventories
                            .Where(wi => wi.WarehouseId == wh.Id && wi.VariantId == reqItem.VariantId)
                            .SumAsync(wi => (decimal?)wi.QuantityAvailable) ?? 0;
                    }

                    bool itemSufficient = available >= reqItem.Quantity;
                    if (!itemSufficient) hasAllItems = false;

                    stockDetails.Add(new
                    {
                        VariantId = reqItem.VariantId,
                        Requested = reqItem.Quantity,
                        Available = available,
                        IsSufficient = itemSufficient
                    });
                }

                string candidateStatus = "DISQUALIFIED";
                string candidateReason = "";

                if (!withinGeofence)
                {
                    candidateStatus = "COLD_CHAIN_VIOLATED";
                    candidateReason = $"Khoảng cách {dist:F2} km vượt quá bán kính bảo quản tối đa {maxColdRadius:F1} km";
                }
                else if (!hasAllItems)
                {
                    candidateStatus = "INSUFFICIENT_STOCK_SKIPPED";
                    candidateReason = "Thiếu hàng trong giỏ -> Bị loại bỏ để chống xé lẻ đơn (Single-Hub Policy)";
                }
                else
                {
                    candidateStatus = "ELIGIBLE";
                    candidateReason = "Đủ 100% hàng và nằm trong bán kính an toàn";

                    // Chọn kho đầu tiên (gần nhất) thỏa mãn cả 2 điều kiện
                    if (selectedWarehouse == null)
                    {
                        selectedWarehouse = wh;
                        selectedDistance = dist;
                        candidateStatus = "SELECTED_OPTIMAL_HUB";
                    }
                }

                candidateEvaluations.Add(new
                {
                    WarehouseId = wh.Id,
                    WarehouseCode = wh.Code,
                    WarehouseName = wh.Name,
                    District = wh.Address?.District,
                    DistanceKm = Math.Round(dist, 2),
                    MaxColdChainRadiusKm = maxColdRadius,
                    WithinColdChainGeofence = withinGeofence,
                    FulfillsFullCart = hasAllItems,
                    Status = candidateStatus,
                    Reason = candidateReason,
                    StockDetails = stockDetails
                });
            }

            sw.Stop();

            // 4. Quyết định kết quả cuối cùng (Final Verdict)
            string overallStatus = "PASS";
            string decisionMessage = "";

            if (selectedWarehouse != null)
            {
                decisionMessage = $"Đã chọn '{selectedWarehouse.Name}' ({selectedDistance:F2} km) làm kho xuất hàng duy nhất (100% giỏ hàng, 1 chuyến xe).";
            }
            else if (cartRequiresColdChain && candidatesWithDistance.All(c => c.DistanceKm > 15.0))
            {
                overallStatus = "BLOCKED_GEOFENCE";
                double nearestDist = candidatesWithDistance.FirstOrDefault()?.DistanceKm ?? 0;
                decisionMessage = $"Từ chối đơn hàng: Có sản phẩm chuỗi lạnh nhưng khoảng cách đến kho gần nhất ({nearestDist:F2} km) vượt quá bán kính 15.0 km.";
            }
            else
            {
                overallStatus = "OUT_OF_STOCK";
                decisionMessage = "Không tìm thấy kho nào đáp ứng đủ 100% giỏ hàng để giao đơn lẻ.";
            }

            return Ok(new
            {
                Success = overallStatus == "PASS",
                Status = overallStatus,
                ExecutionTimeMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
                CartSummary = new
                {
                    TotalItems = request.Items.Count,
                    RequiresColdChain = cartRequiresColdChain,
                    ColdChainItems = itemsInfo.Where(i => i.RequiresColdChain).Select(i => i.VariantName).ToList(),
                    AmbientItems = itemsInfo.Where(i => !i.RequiresColdChain).Select(i => i.VariantName).ToList()
                },
                CustomerLocation = new
                {
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    District = request.District,
                    Province = request.Province
                },
                SelectedHub = selectedWarehouse != null ? new
                {
                    Id = selectedWarehouse.Id,
                    Code = selectedWarehouse.Code,
                    Name = selectedWarehouse.Name,
                    DistanceKm = Math.Round(selectedDistance, 2),
                    ShipmentCount = 1,
                    AvoidedSplitting = true
                } : null,
                DecisionMessage = decisionMessage,
                Candidates = candidateEvaluations
            });
        }
    }

    public class RoutingEvaluationRequestDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
        public List<RoutingEvaluationItemDto> Items { get; set; } = new();
        public int? ExcludeWarehouseStockId { get; set; }
    }

    public class RoutingEvaluationItemDto
    {
        public int VariantId { get; set; }
        public decimal Quantity { get; set; }
    }
}
