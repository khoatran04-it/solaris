using backend.Data;
using backend.DTOs.DashboardDTOs;
using backend.Models;
using backend.Models.Enums;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly SolarisDbContext _db;

        public DashboardService(SolarisDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // Helpers
        // ============================================================
        private (DateTime From, DateTime To) ResolveDateRange(string period, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.UtcNow;
            if (fromDate.HasValue && toDate.HasValue)
                return (fromDate.Value.Date, toDate.Value.Date.AddDays(1).AddTicks(-1));

            return period switch
            {
                "today" => (now.Date, now.Date.AddDays(1).AddTicks(-1)),
                "7days" => (now.Date.AddDays(-6), now.Date.AddDays(1).AddTicks(-1)),
                "year" => (new DateTime(now.Year, 1, 1), now.Date.AddDays(1).AddTicks(-1)),
                _ => (now.Date.AddDays(-29), now.Date.AddDays(1).AddTicks(-1)) // 30days default
            };
        }

        private (DateTime PrevFrom, DateTime PrevTo) GetPreviousPeriod(DateTime from, DateTime to)
        {
            var span = to - from;
            return (from - span, from.AddTicks(-1));
        }

        // ============================================================
        // DASHBOARD 1: OVERVIEW
        // ============================================================
        public async Task<DashboardOverviewDto> GetOverviewAsync(string period, DateTime? fromDate, DateTime? toDate)
        {
            var (from, to) = ResolveDateRange(period, fromDate, toDate);
            var (prevFrom, prevTo) = GetPreviousPeriod(from, to);

            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= from && o.OrderDate <= to)
                .Include(o => o.Customer)
                .ToListAsync();

            var prevOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= prevFrom && o.OrderDate <= prevTo)
                .ToListAsync();

            var totalRevenue = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalAmount);

            var prevRevenue = prevOrders
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalAmount);

            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var totalOrders = orders.Count;
            decimal revenueGrowth = prevRevenue == 0 ? 0
                : Math.Round((totalRevenue - prevRevenue) / prevRevenue * 100, 1);

            // Timeline - group by day/month based on span
            var span = (to - from).TotalDays;
            List<RevenueTimelinePoint> timeline;
            if (span <= 31)
            {
                timeline = orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .GroupBy(o => o.OrderDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new RevenueTimelinePoint
                    {
                        Label = g.Key.ToString("dd/MM"),
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    }).ToList();
            }
            else
            {
                timeline = orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new RevenueTimelinePoint
                    {
                        Label = $"T{g.Key.Month}/{g.Key.Year % 100}",
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    }).ToList();
            }

            // Payment breakdown
            var totalRevForPercent = orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount);
            var paymentBreakdown = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .GroupBy(o => o.PaymentMethod.ToString())
                .Select(g =>
                {
                    var amt = g.Sum(o => o.TotalAmount);
                    return new PaymentMethodBreakdownItem
                    {
                        Method = g.Key,
                        Count = g.Count(),
                        Amount = amt,
                        Percent = totalRevForPercent == 0 ? 0 : Math.Round(amt / totalRevForPercent * 100, 1)
                    };
                }).OrderByDescending(x => x.Amount).ToList();

            // Order status pipeline
            var statusPipeline = orders
                .GroupBy(o => o.Status.ToString())
                .Select(g => new OrderStatusPipelineItem { Status = g.Key, Count = g.Count() })
                .ToList();

            // Recent orders (latest 5)
            var recentOrders = orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new RecentOrderItem
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.Customer?.Name ?? "Khách vãng lai",
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    OrderDate = o.OrderDate
                }).ToList();

            return new DashboardOverviewDto
            {
                TotalRevenue = totalRevenue,
                RevenueGrowthPercent = revenueGrowth,
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                AverageOrderValue = totalOrders == 0 ? 0 : Math.Round(totalRevenue / (completedOrders == 0 ? 1 : completedOrders), 0),
                FulfillmentRatePercent = totalOrders == 0 ? 0 : Math.Round((decimal)completedOrders / totalOrders * 100, 1),
                RevenueTimeline = timeline,
                PaymentMethodBreakdown = paymentBreakdown,
                OrderStatusPipeline = statusPipeline,
                RecentOrders = recentOrders
            };
        }

        // ============================================================
        // DASHBOARD 2: SALES & GEOGRAPHY
        // ============================================================
        public async Task<DashboardSalesGeographyDto> GetSalesGeographyAsync(string period, DateTime? fromDate, DateTime? toDate)
        {
            var (from, to) = ResolveDateRange(period, fromDate, toDate);

            var completedOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed && o.OrderDate >= from && o.OrderDate <= to)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
                                .ThenInclude(c => c!.CategoryGroup)
                .Include(o => o.Details)
                    .ThenInclude(d => d.UoM)
                .Include(o => o.Customer)
                    .ThenInclude(c => c!.CustomerTier)
                .ToListAsync();

            var uomMap = await _db.UoMs.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Name);
            var allDetails = completedOrders.SelectMany(o => o.Details).ToList();
            var totalRev = completedOrders.Sum(o => o.TotalAmount);

            // Top products (quy đổi về Base UoM)
            var topProducts = allDetails
                .Where(d => d.Variant != null)
                .GroupBy(d => new { d.VariantId, d.Variant!.Name, d.Variant.Code })
                .Select(g =>
                {
                    var rev = g.Sum(d => d.UnitPrice * d.Quantity);
                    var baseUomId = g.FirstOrDefault()?.Variant?.Product?.BaseUoMId;
                    var baseUomName = (baseUomId.HasValue && uomMap.TryGetValue(baseUomId.Value, out var bn))
                        ? bn
                        : (g.FirstOrDefault()?.UoM?.Name ?? "");
                    return new TopProductItem
                    {
                        VariantId = g.Key.VariantId,
                        Name = g.Key.Name,
                        Code = g.Key.Code,
                        UoM = baseUomName,
                        QuantitySold = (int)g.Sum(d => d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity),
                        TotalRevenue = rev,
                        RevenuePercent = totalRev == 0 ? 0 : Math.Round(rev / totalRev * 100, 1)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            // Category breakdown
            var categoryBreakdown = allDetails
                .Where(d => d.Variant?.Product?.Category?.CategoryGroup != null)
                .GroupBy(d => d.Variant!.Product!.Category!.CategoryGroup!.Name)
                .Select(g =>
                {
                    var rev = g.Sum(d => d.UnitPrice * d.Quantity);
                    return new CategoryBreakdownItem
                    {
                        CategoryGroupName = g.Key,
                        TotalRevenue = rev,
                        QuantitySold = (int)g.Sum(d => d.Quantity),
                        Percent = totalRev == 0 ? 0 : Math.Round(rev / totalRev * 100, 1)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            // Geography breakdown - from snapshot DeliveryAddress (province extraction)
            var geographyBreakdown = completedOrders
                .Where(o => !string.IsNullOrWhiteSpace(o.DeliveryAddress))
                .GroupBy(o =>
                {
                    // DeliveryAddress snapshot: "street, ward, district, province"
                    var parts = o.DeliveryAddress!.Split(',');
                    return parts.Length >= 1 ? parts[^1].Trim() : "Khác";
                })
                .Select(g =>
                {
                    var rev = g.Sum(o => o.TotalAmount);
                    return new GeographyBreakdownItem
                    {
                        ProvinceName = g.Key,
                        OrderCount = g.Count(),
                        TotalRevenue = rev,
                        Percent = totalRev == 0 ? 0 : Math.Round(rev / totalRev * 100, 1)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(20)
                .ToList();

            // Customer tier breakdown
            var tierBreakdown = completedOrders
                .GroupBy(o => o.Customer?.CustomerTier?.Name ?? "Chưa phân hạng")
                .Select(g => new CustomerTierBreakdownItem
                {
                    TierName = g.Key,
                    CustomerCount = g.Select(o => o.CustomerId).Distinct().Count(),
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    Percent = totalRev == 0 ? 0 : Math.Round(g.Sum(o => o.TotalAmount) / totalRev * 100, 1)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            return new DashboardSalesGeographyDto
            {
                TopProducts = topProducts,
                CategoryBreakdown = categoryBreakdown,
                GeographyBreakdown = geographyBreakdown,
                CustomerTierBreakdown = tierBreakdown
            };
        }

        // ============================================================
        // DASHBOARD 3: INVENTORY & CAPACITY
        // ============================================================
        public async Task<DashboardInventoryCapacityDto> GetInventoryCapacityAsync()
        {
            var warehouses = await _db.Warehouses
                .AsNoTracking()
                .Where(w => !w.IsDeleted && w.IsActive)
                .ToListAsync();

            var inventories = await _db.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Variant)
                .Include(wi => wi.Warehouse)
                .ToListAsync();

            var prices = await _db.ProductVariantPrices
                .AsNoTracking()
                .ToListAsync();

            var productBaseUoms = await _db.Products
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => p.BaseUoMId);
            var variantProducts = await _db.ProductVariants
                .AsNoTracking()
                .ToDictionaryAsync(v => v.Id, v => v.ProductId);

            // Warehouse capacities
            var warehouseCapacities = warehouses.Select(w =>
            {
                var inv = inventories.Where(i => i.WarehouseId == w.Id).ToList();
                var totalQty = inv.Sum(i => i.QuantityAvailable + i.QuantityReserved + i.QuantityQC + i.QuantityDamaged);
                var occupiedCbm = (decimal)0;
                var occupiedKg = (decimal)0;
                foreach (var i in inv)
                {
                    var totalUnitQty = i.QuantityAvailable + i.QuantityReserved + i.QuantityQC + i.QuantityDamaged;
                    occupiedCbm += CalculateUnitCbm(i.Variant) * totalUnitQty;
                    occupiedKg += CalculateUnitWeightKg(i.Variant) * totalUnitQty;
                }
                var cbmPercent = (w.TotalCapacityCbm ?? 0) > 0
                    ? Math.Max(occupiedCbm > 0 ? 0.01m : 0m, Math.Round(occupiedCbm / (w.TotalCapacityCbm ?? 1) * 100, 2))
                    : 0;
                var kgPercent = (w.MaxWeightCapacityKg ?? 0) > 0
                    ? Math.Max(occupiedKg > 0 ? 0.01m : 0m, Math.Round(occupiedKg / (w.MaxWeightCapacityKg ?? 1) * 100, 2))
                    : 0;
                string status = cbmPercent >= 95 || kgPercent >= 95 ? "Critical"
                    : cbmPercent >= w.WarningThresholdPercent || kgPercent >= w.WarningThresholdPercent ? "Warning"
                    : "Safe";

                return new WarehouseCapacityItem
                {
                    WarehouseId = w.Id,
                    WarehouseName = w.Name,
                    WarehouseCode = w.Code,
                    TotalCbm = w.TotalCapacityCbm ?? 0,
                    OccupiedCbm = Math.Round(occupiedCbm, 2),
                    OccupancyCbmPercent = cbmPercent,
                    MaxWeightKg = w.MaxWeightCapacityKg ?? 0,
                    OccupiedWeightKg = Math.Round(occupiedKg, 1),
                    OccupancyWeightPercent = kgPercent,
                    Status = status,
                    TotalAreaSqm = w.TotalAreaSqm ?? 0,
                    WarningThresholdPercent = w.WarningThresholdPercent
                };
            }).ToList();

            // Compartments
            var compartments = new InventoryCompartmentsDto
            {
                AvailableQty = (int)inventories.Sum(i => i.QuantityAvailable),
                ReservedQty = (int)inventories.Sum(i => i.QuantityReserved),
                InQcQty = (int)inventories.Sum(i => i.QuantityQC),
                DamagedQty = (int)inventories.Sum(i => i.QuantityDamaged)
            };

            // Total stock value (Available * SellPrice theo Base UoM)
            decimal totalValue = 0;
            foreach (var inv in inventories)
            {
                int? baseUomId = null;
                if (variantProducts.TryGetValue(inv.VariantId, out var prodId) && productBaseUoms.TryGetValue(prodId, out var bUomId))
                {
                    baseUomId = bUomId;
                }
                var price = prices
                    .Where(p => p.VariantId == inv.VariantId && (!baseUomId.HasValue || p.UoMId == baseUomId.Value))
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()
                    ?? prices
                    .Where(p => p.VariantId == inv.VariantId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();
                totalValue += (price?.Price ?? 0) * inv.QuantityAvailable;
            }
            compartments.TotalValue = totalValue;

            // Top space-consuming
            var topSpace = inventories
                .Where(i => i.Variant != null)
                .GroupBy(i => new { i.VariantId, i.Variant!.Name, i.Variant.Code })
                .Select(g =>
                {
                    var variant = inventories.First(i => i.VariantId == g.Key.VariantId).Variant;
                    var unitCbm = CalculateUnitCbm(variant);
                    var totalQty = (int)g.Sum(i => i.QuantityAvailable + i.QuantityReserved + i.QuantityQC + i.QuantityDamaged);
                    return new TopSpaceConsumingItem
                    {
                        VariantName = g.Key.Name,
                        VariantCode = g.Key.Code,
                        TotalQty = totalQty,
                        TotalCbm = Math.Round(unitCbm * totalQty, 2)
                    };
                })
                .Where(x => x.TotalCbm > 0)
                .OrderByDescending(x => x.TotalCbm)
                .Take(5)
                .ToList();

            // Low stock alerts
            var lowStock = inventories
                .Where(i => i.Variant != null && i.QuantityAvailable < i.Variant.InventoryGuideline && i.Variant.InventoryGuideline > 0)
                .Select(i => new LowStockAlertItem
                {
                    VariantName = i.Variant!.Name,
                    VariantCode = i.Variant.Code,
                    WarehouseName = i.Warehouse?.Name ?? "",
                    AvailableQty = (int)i.QuantityAvailable
                })
                .OrderBy(x => x.AvailableQty)
                .Take(10)
                .ToList();

            return new DashboardInventoryCapacityDto
            {
                TotalStockValue = totalValue,
                TotalActiveWarehouses = warehouses.Count,
                WarehouseCapacities = warehouseCapacities,
                InventoryCompartments = compartments,
                TopSpaceConsumingProducts = topSpace,
                LowStockAlerts = lowStock
            };
        }

        // ============================================================
        // DASHBOARD 4: QUALITY & EXPIRY
        // ============================================================
        public async Task<DashboardQualityExpiryDto> GetQualityExpiryAsync()
        {
            var today = DateTime.UtcNow.Date;

            // Inventory with batch info (for expiry)
            var inventories = await _db.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Batch)
                .Include(wi => wi.Variant)
                .Include(wi => wi.Warehouse)
                .ToListAsync();

            var prices = await _db.ProductVariantPrices
                .AsNoTracking()
                .ToListAsync();

            var products = await _db.Products
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Id, p => new { p.Name, p.BaseUoMId });
            var variantProducts = await _db.ProductVariants
                .AsNoTracking()
                .ToDictionaryAsync(v => v.Id, v => v.ProductId);

            // Filter active batches with stock
            var activeBatches = inventories
                .Where(i => i.Batch != null && (i.QuantityAvailable + i.QuantityQC) > 0)
                .ToList();

            // Expiry overview
            var expired = activeBatches.Count(i => i.Batch!.ExpiryDate.Date < today);
            var critical = activeBatches.Count(i => i.Batch!.ExpiryDate.Date >= today && (i.Batch.ExpiryDate.Date - today).TotalDays < 3);
            var warning = activeBatches.Count(i => i.Batch!.ExpiryDate.Date >= today && (i.Batch.ExpiryDate.Date - today).TotalDays >= 3 && (i.Batch.ExpiryDate.Date - today).TotalDays < 7);
            var safe = activeBatches.Count(i => i.Batch!.ExpiryDate.Date >= today && (i.Batch.ExpiryDate.Date - today).TotalDays >= 7);

            // Expiring batches list (expired + critical + warning, sorted by nearest)
            var expiringBatches = activeBatches
                .Where(i => (i.Batch!.ExpiryDate.Date - today).TotalDays < 7)
                .OrderBy(i => i.Batch!.ExpiryDate)
                .Select(i =>
                {
                    var daysRemaining = (int)(i.Batch!.ExpiryDate.Date - today).TotalDays;
                    int? baseUomId = null;
                    string productName = "";
                    if (variantProducts.TryGetValue(i.VariantId, out var prodId) && products.TryGetValue(prodId, out var pInfo))
                    {
                        baseUomId = pInfo.BaseUoMId;
                        productName = pInfo.Name;
                    }
                    var price = prices
                        .Where(p => p.VariantId == i.VariantId && (!baseUomId.HasValue || p.UoMId == baseUomId.Value))
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault()
                        ?? prices
                        .Where(p => p.VariantId == i.VariantId)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault();
                    var unitPrice = price?.Price ?? 0;
                    return new ExpiringBatchItem
                    {
                        BatchCode = i.Batch.BatchCode,
                        ProductName = productName,
                        VariantName = i.Variant?.Name ?? "",
                        WarehouseName = i.Warehouse?.Name ?? "",
                        ExpiryDate = i.Batch.ExpiryDate,
                        DaysRemaining = daysRemaining,
                        QuantityAvailable = (int)i.QuantityAvailable,
                        UnitPrice = unitPrice,
                        EstimatedLossValue = unitPrice * i.QuantityAvailable
                    };
                })
                .Take(20)
                .ToList();

            // Inbound QC reject rate (from InventoryReceiptDetails)
            var receiptDetails = await _db.InventoryReceiptDetails
                .AsNoTracking()
                .ToListAsync();
            var totalInbound = (int)receiptDetails.Sum(d => d.AcceptedQuantity + d.RejectedQuantity);
            var totalRejected = (int)receiptDetails.Sum(d => d.RejectedQuantity);
            decimal qcRejectRate = totalInbound == 0 ? 0 : Math.Round((decimal)totalRejected / totalInbound * 100, 1);

            // QC reject reasons
            var qcReasons = receiptDetails
                .Where(d => d.RejectedQuantity > 0 && !string.IsNullOrWhiteSpace(d.RejectReason))
                .GroupBy(d => d.RejectReason!)
                .Select(g =>
                {
                    var count = g.Count();
                    return new QcRejectReasonItem
                    {
                        Reason = g.Key,
                        Count = count,
                        Percent = totalRejected == 0 ? 0 : Math.Round((decimal)count / (receiptDetails.Count(d => d.RejectedQuantity > 0)) * 100, 1)
                    };
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // Customer return rate
            var totalOrders = await _db.Orders.AsNoTracking().CountAsync(o => !o.IsDeleted);
            var totalReturns = await _db.CustomerReturns.AsNoTracking().CountAsync(r => !r.IsDeleted);
            decimal returnRate = totalOrders == 0 ? 0 : Math.Round((decimal)totalReturns / totalOrders * 100, 1);

            return new DashboardQualityExpiryDto
            {
                ExpiryOverview = new ExpiryOverviewDto
                {
                    ExpiredCount = expired,
                    CriticalCount = critical,
                    WarningCount = warning,
                    SafeCount = safe
                },
                InboundQcRejectRatePercent = qcRejectRate,
                TotalInboundItems = totalInbound,
                TotalRejectedItems = totalRejected,
                CustomerReturnRatePercent = returnRate,
                TotalOrders = totalOrders,
                TotalReturnOrders = totalReturns,
                ExpiringBatches = expiringBatches,
                QcRejectReasons = qcReasons
            };
        }

        // ============================================================
        // DASHBOARD 5: FINANCIAL & CASH FLOW PERFORMANCE
        // ============================================================
        public async Task<DashboardFinancialPerformanceDto> GetFinancialPerformanceAsync(string period, DateTime? fromDate, DateTime? toDate)
        {
            var (from, to) = ResolveDateRange(period, fromDate, toDate);
            var (prevFrom, prevTo) = GetPreviousPeriod(from, to);

            // 1. Đơn hàng bán trong kỳ
            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= from && o.OrderDate <= to)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
                                .ThenInclude(c => c!.CategoryGroup)
                .ToListAsync();

            // Đơn hàng bán kỳ trước (để tính tăng trưởng)
            var prevCompletedOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed && o.OrderDate >= prevFrom && o.OrderDate <= prevTo)
                .ToListAsync();

            // 2. Đơn trả hàng (RMA) trong kỳ
            var returns = await _db.CustomerReturns
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.ReturnDate >= from && r.ReturnDate <= to)
                .ToListAsync();

            // 3. Đơn mua hàng PO trong kỳ
            var purchaseOrders = await _db.PurchaseOrders
                .AsNoTracking()
                .Where(po => !po.IsDeleted && po.OrderDate >= from && po.OrderDate <= to)
                .Include(po => po.Supplier)
                .Include(po => po.Details)
                .ToListAsync();

            // 4. Phiếu nhập kho trong kỳ
            var receipts = await _db.InventoryReceipts
                .AsNoTracking()
                .Where(ir => !ir.IsDeleted && ir.CreatedAt >= from && ir.CreatedAt <= to)
                .Include(ir => ir.Details)
                .Include(ir => ir.Supplier)
                .ToListAsync();

            // 5. Giá vốn tham chiếu từ PO và SupplierProduct
            var latestPoPrices = await _db.PurchaseOrderDetails
                .AsNoTracking()
                .Where(pod => pod.PurchaseOrder != null && !pod.PurchaseOrder.IsDeleted && pod.UnitPrice > 0)
                .OrderByDescending(pod => pod.PurchaseOrder!.OrderDate)
                .GroupBy(pod => pod.VariantId)
                .Select(g => new { VariantId = g.Key, UnitPrice = g.First().UnitPrice })
                .ToDictionaryAsync(x => x.VariantId, x => x.UnitPrice);

            var supplierProductPrices = await _db.SupplierProducts
                .AsNoTracking()
                .Where(sp => !sp.IsDeleted && sp.IsActive && sp.LastImportPrice > 0)
                .GroupBy(sp => sp.VariantId)
                .Select(g => new { VariantId = g.Key, LastImportPrice = g.First().LastImportPrice })
                .ToDictionaryAsync(x => x.VariantId, x => x.LastImportPrice);

            // Bảng tồn kho hàng hỏng và hết date
            var today = DateTime.UtcNow.Date;
            var inventories = await _db.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Batch)
                .ToListAsync();

            // --- TÍNH TOÁN CHỈ SỐ DOANH THU & GIÁ VỐN ---
            var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
            var completedReturns = returns.Where(r => r.Status == CustomerReturnStatus.Completed).ToList();

            decimal grossRevenue = completedOrders.Sum(o => o.TotalAmount);
            decimal customerRefunds = completedReturns.Sum(r => r.RefundAmount);
            decimal netRevenue = Math.Max(0, grossRevenue - customerRefunds);

            decimal prevGrossRev = prevCompletedOrders.Sum(o => o.TotalAmount);
            decimal netRevenueGrowth = prevGrossRev == 0 ? 0 : Math.Round((netRevenue - prevGrossRev) / prevGrossRev * 100, 1);

            // Tính COGS từng dòng mặt hàng của đơn đã hoàn tất
            decimal totalCogs = 0;
            var categoryMap = new Dictionary<string, (decimal Revenue, decimal Cogs, int Qty)>();

            foreach (var ord in completedOrders)
            {
                foreach (var d in ord.Details)
                {
                    decimal unitCost = 0;
                    if (latestPoPrices.TryGetValue(d.VariantId, out var poCost) && poCost > 0)
                        unitCost = poCost;
                    else if (supplierProductPrices.TryGetValue(d.VariantId, out var spCost) && spCost > 0)
                        unitCost = spCost;
                    else
                        unitCost = d.UnitPrice * 0.70m; // Fallback 70% giá bán

                    decimal qty = d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity;
                    decimal lineCogs = unitCost * qty;
                    decimal lineRev = d.UnitPrice * d.Quantity - d.DiscountAmount;

                    totalCogs += lineCogs;

                    var catName = d.Variant?.Product?.Category?.CategoryGroup?.Name ?? "Nông sản tổng hợp";
                    if (!categoryMap.ContainsKey(catName))
                        categoryMap[catName] = (0, 0, 0);

                    var current = categoryMap[catName];
                    categoryMap[catName] = (current.Revenue + lineRev, current.Cogs + lineCogs, current.Qty + (int)qty);
                }
            }

            decimal grossProfit = netRevenue - totalCogs;
            decimal grossMarginPercent = netRevenue == 0 ? 0 : Math.Round(grossProfit / netRevenue * 100, 1);

            // --- TÍNH TOÁN CẦU NỐI DÒNG TIỀN (CASH FLOW BRIDGE) ---
            decimal onlineInflow = orders
                .Where(o => (o.PaymentStatus == PaymentStatus.Paid) &&
                            (o.PaymentMethod == PaymentMethod.BankTransfer || o.PaymentMethod == PaymentMethod.EWallet || o.PaymentMethod == PaymentMethod.CreditCard))
                .Sum(o => o.TotalAmount);

            decimal codCollectedInflow = completedOrders
                .Where(o => o.PaymentMethod == PaymentMethod.COD)
                .Sum(o => o.TotalAmount);

            decimal codInTransit = orders
                .Where(o => o.Status == OrderStatus.Shipping && o.PaymentMethod == PaymentMethod.COD)
                .Sum(o => o.TotalAmount);

            decimal totalPoValue = purchaseOrders
                .Where(po => po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.Completed || po.Status == PurchaseOrderStatus.PartiallyReceived)
                .Sum(po => po.TotalAmount);

            decimal goodsReceivedValue = 0;
            decimal qcRejectedValue = 0;

            foreach (var rec in receipts.Where(r => r.Status == InventoryReceiptStatus.Completed))
            {
                foreach (var d in rec.Details)
                {
                    decimal cost = 0;
                    if (latestPoPrices.TryGetValue(d.VariantId, out var poCost) && poCost > 0)
                        cost = poCost;
                    else if (supplierProductPrices.TryGetValue(d.VariantId, out var spCost) && spCost > 0)
                        cost = spCost;
                    else
                        cost = 50000;

                    goodsReceivedValue += d.AcceptedQuantity * cost;
                    qcRejectedValue += d.RejectedQuantity * cost;
                }
            }

            if (goodsReceivedValue == 0)
            {
                goodsReceivedValue = purchaseOrders
                    .Where(po => po.Status == PurchaseOrderStatus.Completed)
                    .Sum(po => po.TotalAmount);
            }

            decimal pendingPoCommitment = Math.Max(0, totalPoValue - goodsReceivedValue);
            decimal realInflow = onlineInflow + codCollectedInflow;
            decimal estimatedNetCashFlow = realInflow - customerRefunds - goodsReceivedValue;

            var cashFlowBridge = new CashFlowBridgeDto
            {
                GrossSales = grossRevenue,
                OnlinePaymentInflow = onlineInflow,
                CodCollectedInflow = codCollectedInflow,
                CodInTransitAmount = codInTransit,
                CustomerRefundOutflow = customerRefunds,
                InboundGoodsReceiptOutflow = goodsReceivedValue,
                PendingPoCommitment = pendingPoCommitment,
                NetOperatingCashFlow = estimatedNetCashFlow
            };

            // --- BẢNG ĐỐI SOÁT THEO NHÀ CUNG CẤP (SUPPLIER PAYABLES) ---
            var supplierPayables = purchaseOrders
                .Where(po => po.Supplier != null)
                .GroupBy(po => new { po.SupplierId, po.Supplier!.Name, po.Supplier.Code })
                .Select(g =>
                {
                    var poCount = g.Count();
                    var poVal = g.Sum(p => p.TotalAmount);
                    var recVal = g.Where(p => p.Status == PurchaseOrderStatus.Completed).Sum(p => p.TotalAmount);
                    var pending = Math.Max(0, poVal - recVal);
                    return new SupplierPayableItem
                    {
                        SupplierId = g.Key.SupplierId,
                        SupplierName = g.Key.Name,
                        SupplierCode = g.Key.Code,
                        TotalPoCount = poCount,
                        TotalPoValue = poVal,
                        ReceivedValue = recVal,
                        QcRejectedValue = 0,
                        PendingCommitment = pending
                    };
                })
                .OrderByDescending(x => x.TotalPoValue)
                .Take(10)
                .ToList();

            if (!supplierPayables.Any())
            {
                var topSuppliers = await _db.Suppliers.AsNoTracking().Where(s => !s.IsDeleted && s.IsActive).Take(5).ToListAsync();
                supplierPayables = topSuppliers.Select(s => new SupplierPayableItem
                {
                    SupplierId = s.Id,
                    SupplierName = s.Name,
                    SupplierCode = s.Code,
                    TotalPoCount = 0,
                    TotalPoValue = 0,
                    ReceivedValue = 0,
                    QcRejectedValue = 0,
                    PendingCommitment = 0
                }).ToList();
            }

            // --- HIỆU SUẤT THEO NGÀNH HÀNG (CATEGORY PROFITABILITY) ---
            var categoryProfitability = categoryMap.Select(kvp =>
            {
                var profit = kvp.Value.Revenue - kvp.Value.Cogs;
                var margin = kvp.Value.Revenue == 0 ? 0 : Math.Round(profit / kvp.Value.Revenue * 100, 1);
                return new CategoryProfitabilityItem
                {
                    CategoryGroupName = kvp.Key,
                    Revenue = kvp.Value.Revenue,
                    Cogs = kvp.Value.Cogs,
                    GrossProfit = profit,
                    GrossMarginPercent = margin,
                    QuantitySold = kvp.Value.Qty
                };
            }).OrderByDescending(x => x.GrossProfit).ToList();

            // --- TỔN THẤT HAO HỤT & CHẤT LƯỢNG (SHRINKAGE LOSS) ---
            decimal damagedStockValue = 0;
            decimal expiringRiskValue = 0;
            foreach (var inv in inventories)
            {
                decimal cost = 0;
                if (latestPoPrices.TryGetValue(inv.VariantId, out var poCost) && poCost > 0)
                    cost = poCost;
                else if (supplierProductPrices.TryGetValue(inv.VariantId, out var spCost) && spCost > 0)
                    cost = spCost;
                else
                    cost = 40000;

                if (inv.QuantityDamaged > 0)
                    damagedStockValue += inv.QuantityDamaged * cost;

                if (inv.Batch != null && (inv.QuantityAvailable + inv.QuantityQC) > 0)
                {
                    var daysToExpiry = (inv.Batch.ExpiryDate.Date - today).TotalDays;
                    if (daysToExpiry >= 0 && daysToExpiry <= 7)
                        expiringRiskValue += (inv.QuantityAvailable + inv.QuantityQC) * cost;
                }
            }

            var shrinkageLoss = new ShrinkageLossDto
            {
                DamagedStockValue = damagedStockValue,
                ExpiringStockRiskValue = expiringRiskValue,
                ReturnRefundLoss = customerRefunds,
                TotalShrinkageLoss = damagedStockValue + expiringRiskValue + customerRefunds
            };

            // --- TIMELINE: DOANH THU VS GIÁ VỐN VS LỢI NHUẬN ---
            var span = (to - from).TotalDays;
            List<FinancialTimelinePoint> timeline;
            if (span <= 31)
            {
                timeline = completedOrders
                    .GroupBy(o => o.OrderDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        var rev = g.Sum(o => o.TotalAmount);
                        decimal dayCogs = 0;
                        foreach (var o in g)
                        {
                            foreach (var d in o.Details)
                            {
                                decimal c = latestPoPrices.GetValueOrDefault(d.VariantId, supplierProductPrices.GetValueOrDefault(d.VariantId, d.UnitPrice * 0.7m));
                                dayCogs += c * (d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity);
                            }
                        }
                        return new FinancialTimelinePoint
                        {
                            Label = g.Key.ToString("dd/MM"),
                            Revenue = rev,
                            Cogs = dayCogs,
                            GrossProfit = rev - dayCogs,
                            CashInflow = rev,
                            CashOutflow = dayCogs
                        };
                    }).ToList();
            }
            else
            {
                timeline = completedOrders
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g =>
                    {
                        var rev = g.Sum(o => o.TotalAmount);
                        decimal monthCogs = 0;
                        foreach (var o in g)
                        {
                            foreach (var d in o.Details)
                            {
                                decimal c = latestPoPrices.GetValueOrDefault(d.VariantId, supplierProductPrices.GetValueOrDefault(d.VariantId, d.UnitPrice * 0.7m));
                                monthCogs += c * (d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity);
                            }
                        }
                        return new FinancialTimelinePoint
                        {
                            Label = $"T{g.Key.Month}/{g.Key.Year % 100}",
                            Revenue = rev,
                            Cogs = monthCogs,
                            GrossProfit = rev - monthCogs,
                            CashInflow = rev,
                            CashOutflow = monthCogs
                        };
                    }).ToList();
            }

            return new DashboardFinancialPerformanceDto
            {
                GrossRevenue = grossRevenue,
                CustomerRefunds = customerRefunds,
                NetRevenue = netRevenue,
                TotalCogs = totalCogs,
                GrossProfit = grossProfit,
                GrossMarginPercent = grossMarginPercent,
                TotalPoValue = totalPoValue,
                TotalGoodsReceivedValue = goodsReceivedValue,
                EstimatedNetCashFlow = estimatedNetCashFlow,
                NetRevenueGrowthPercent = netRevenueGrowth,
                CashFlowBridge = cashFlowBridge,
                Timeline = timeline,
                SupplierPayables = supplierPayables,
                CategoryProfitability = categoryProfitability,
                ShrinkageLoss = shrinkageLoss
            };
        }

        private static decimal CalculateUnitCbm(ProductVariant? variant)
        {
            if (variant == null) return 0.01m;
            if (variant.UnitCbm.HasValue && variant.UnitCbm.Value > 0)
                return variant.UnitCbm.Value;

            if (variant.LengthCm > 0 && variant.WidthCm > 0 && variant.HeightCm > 0)
                return Math.Round((variant.LengthCm.Value * variant.WidthCm.Value * variant.HeightCm.Value) / 1000000m, 4);

            if (variant.GrossWeightKg.HasValue && variant.GrossWeightKg.Value > 0)
            {
                // Ước lượng nông sản / thực phẩm tiêu dùng trung bình tỷ trọng 500-1000 kg/m3 (0.002 m3/kg)
                return Math.Max(0.0005m, Math.Round(variant.GrossWeightKg.Value * 0.002m, 4));
            }

            return 0.01m;
        }

        private static decimal CalculateUnitWeightKg(ProductVariant? variant)
        {
            if (variant == null) return 1.0m;
            if (variant.GrossWeightKg.HasValue && variant.GrossWeightKg.Value > 0)
                return variant.GrossWeightKg.Value;

            return 1.0m;
        }

        // ============================================================
        // DASHBOARD 6: PRICE VOLATILITY & MARGIN SPREAD
        // ============================================================

        public async Task<List<SkuSelectItemDto>> GetPriceVolatilitySkusAsync()
        {
            var variants = await _db.ProductVariants
                .AsNoTracking()
                .Where(v => !v.IsDeleted && v.IsActive)
                .Include(v => v.Product)
                    .ThenInclude(p => p!.BaseUoM)
                .OrderBy(v => v.Name)
                .Select(v => new SkuSelectItemDto
                {
                    VariantId = v.Id,
                    VariantName = v.Name,
                    VariantCode = v.Code,
                    ProductName = v.Product != null ? v.Product.Name : string.Empty,
                    BaseUoMName = v.Product != null && v.Product.BaseUoM != null ? v.Product.BaseUoM.Name : "Kg"
                })
                .ToListAsync();

            return variants;
        }

        public async Task<DashboardPriceVolatilityDto> GetPriceVolatilityAsync(int? variantId, string timeframe, DateTime? fromDate, DateTime? toDate)
        {
            // 1. Xác định SKU cần xem
            int targetVariantId = 0;
            if (variantId.HasValue && variantId.Value > 0)
            {
                targetVariantId = variantId.Value;
            }
            else
            {
                var popularVariantId = await _db.OrderDetails
                    .Where(od => !od.Order.IsDeleted && od.Order.Status != OrderStatus.Cancelled)
                    .GroupBy(od => od.VariantId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefaultAsync();

                if (popularVariantId > 0)
                {
                    targetVariantId = popularVariantId;
                }
                else
                {
                    targetVariantId = await _db.ProductVariants
                        .Where(v => !v.IsDeleted && v.IsActive)
                        .Select(v => v.Id)
                        .FirstOrDefaultAsync();
                }
            }

            var variant = await _db.ProductVariants
                .AsNoTracking()
                .Where(v => v.Id == targetVariantId)
                .Include(v => v.Product)
                    .ThenInclude(p => p!.BaseUoM)
                .Include(v => v.Prices.Where(p => !p.IsDeleted && p.IsActive))
                    .ThenInclude(pr => pr.UoM)
                .FirstOrDefaultAsync();

            if (variant == null)
            {
                return new DashboardPriceVolatilityDto();
            }

            int baseUoMId = variant.Product?.BaseUoMId ?? 0;
            string baseUoMName = variant.Product?.BaseUoM?.Name ?? "Kg";

            // 2. Tải bảng quy đổi UoM
            var conversions = await _db.UoMConversions
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive && (c.ProductId == null || c.ProductId == variant.ProductId))
                .ToListAsync();

            (decimal normPrice, decimal normQty) Normalize(int uomId, decimal unitPrice, decimal qty)
            {
                if (baseUoMId == 0 || uomId == baseUoMId)
                    return (unitPrice, qty);

                var conv = conversions.FirstOrDefault(c => c.FromUoMId == uomId && c.ToUoMId == baseUoMId);
                if (conv != null && conv.ConversionFactor > 0)
                {
                    return (Math.Round(unitPrice / conv.ConversionFactor, 2), Math.Round(qty * conv.ConversionFactor, 2));
                }

                var revConv = conversions.FirstOrDefault(c => c.ToUoMId == uomId && c.FromUoMId == baseUoMId);
                if (revConv != null && revConv.ConversionFactor > 0)
                {
                    return (Math.Round(unitPrice * revConv.ConversionFactor, 2), Math.Round(qty / revConv.ConversionFactor, 2));
                }

                return (unitPrice, qty);
            }

            // 3. Xác định khoảng thời gian
            var now = DateTime.UtcNow;
            DateTime from;
            DateTime to;

            if (fromDate.HasValue && toDate.HasValue)
            {
                from = fromDate.Value.Date;
                to = toDate.Value.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                switch (timeframe?.ToLower())
                {
                    case "week":
                        from = now.Date.AddDays(-84); // 12 tuần
                        to = now.Date.AddDays(1).AddTicks(-1);
                        break;
                    case "year":
                        from = new DateTime(now.Year - 1, 1, 1); // 2 năm
                        to = now.Date.AddDays(1).AddTicks(-1);
                        break;
                    case "month":
                    default:
                        from = now.Date.AddDays(-180); // 6 tháng
                        to = now.Date.AddDays(1).AddTicks(-1);
                        break;
                }
            }

            // 4. Lấy dữ liệu Đơn mua hàng (PO - Nhập hàng)
            var poDetails = await _db.PurchaseOrderDetails
                .AsNoTracking()
                .Where(pod => pod.VariantId == targetVariantId &&
                              !pod.PurchaseOrder.IsDeleted &&
                              pod.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled &&
                              pod.PurchaseOrder.Status != PurchaseOrderStatus.Draft &&
                              pod.PurchaseOrder.OrderDate >= from &&
                              pod.PurchaseOrder.OrderDate <= to)
                .Include(pod => pod.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(pod => pod.UoM)
                .OrderBy(pod => pod.PurchaseOrder.OrderDate)
                .ToListAsync();

            // 5. Lấy dữ liệu Đơn bán hàng (Order - Bán hàng)
            var orderDetails = await _db.OrderDetails
                .AsNoTracking()
                .Where(od => od.VariantId == targetVariantId &&
                             !od.Order.IsDeleted &&
                             od.Order.Status != OrderStatus.Cancelled &&
                             od.Order.OrderDate >= from &&
                             od.Order.OrderDate <= to)
                .Include(od => od.Order)
                    .ThenInclude(o => o.Customer)
                .Include(od => od.UoM)
                .OrderBy(od => od.Order.OrderDate)
                .ToListAsync();

            // 6. Tính thẻ tóm tắt
            decimal latestImportPrice = 0;
            decimal prevImportPrice = 0;
            if (poDetails.Count > 0)
            {
                var latestPo = poDetails.Last();
                var normLatest = Normalize(latestPo.UoMId, latestPo.UnitPrice, latestPo.OrderQuantity);
                latestImportPrice = normLatest.normPrice;

                if (poDetails.Count > 1)
                {
                    var prevPo = poDetails[poDetails.Count - 2];
                    var normPrev = Normalize(prevPo.UoMId, prevPo.UnitPrice, prevPo.OrderQuantity);
                    prevImportPrice = normPrev.normPrice;
                }
                else
                {
                    prevImportPrice = latestImportPrice;
                }
            }
            else
            {
                var supProd = await _db.SupplierProducts
                    .Where(sp => sp.VariantId == targetVariantId)
                    .Select(sp => sp.LastImportPrice)
                    .FirstOrDefaultAsync();
                latestImportPrice = supProd;
                prevImportPrice = supProd;
            }

            decimal currentSellingPrice = 0;
            if (orderDetails.Count > 0)
            {
                var latestOrder = orderDetails.Last();
                var normLatestSell = Normalize(latestOrder.UoMId, latestOrder.UnitPrice, latestOrder.Quantity);
                currentSellingPrice = normLatestSell.normPrice;
            }
            else
            {
                var defaultPrice = variant.Prices.FirstOrDefault(p => p.IsDefault) ?? variant.Prices.FirstOrDefault();
                if (defaultPrice != null)
                {
                    var normDefault = Normalize(defaultPrice.UoMId, defaultPrice.Price, 1);
                    currentSellingPrice = normDefault.normPrice;
                }
            }

            decimal priceSpread = currentSellingPrice - latestImportPrice;
            decimal marginPercent = currentSellingPrice > 0 ? Math.Round(priceSpread / currentSellingPrice * 100, 1) : 0;
            decimal importChangePercent = prevImportPrice > 0 ? Math.Round((latestImportPrice - prevImportPrice) / prevImportPrice * 100, 1) : 0;

            // 7. Nhóm mốc thời gian (Timeline Points)
            var timelinePoints = new List<PriceVolatilityPointDto>();
            var tf = timeframe?.ToLower() ?? "month";

            if (tf == "week")
            {
                var weekStart = from.Date.AddDays(-(int)from.Date.DayOfWeek + (int)DayOfWeek.Monday);
                while (weekStart <= to.Date)
                {
                    var weekEnd = weekStart.AddDays(7).AddTicks(-1);
                    string label = $"T{GetIsoWeek(weekStart)} ({weekStart:dd/MM})";

                    var weekImports = poDetails
                        .Where(p => p.PurchaseOrder.OrderDate >= weekStart && p.PurchaseOrder.OrderDate <= weekEnd)
                        .Select(p => Normalize(p.UoMId, p.UnitPrice, p.OrderQuantity))
                        .ToList();

                    var weekSales = orderDetails
                        .Where(o => o.Order.OrderDate >= weekStart && o.Order.OrderDate <= weekEnd)
                        .Select(o => Normalize(o.UoMId, o.UnitPrice, o.Quantity))
                        .ToList();

                    decimal avgImp = weekImports.Count > 0 && weekImports.Sum(x => x.normQty) > 0
                        ? Math.Round(weekImports.Sum(x => x.normPrice * x.normQty) / weekImports.Sum(x => x.normQty), 0)
                        : (timelinePoints.Count > 0 ? timelinePoints.Last().AvgImportPrice : latestImportPrice);

                    decimal avgSell = weekSales.Count > 0 && weekSales.Sum(x => x.normQty) > 0
                        ? Math.Round(weekSales.Sum(x => x.normPrice * x.normQty) / weekSales.Sum(x => x.normQty), 0)
                        : (timelinePoints.Count > 0 ? timelinePoints.Last().AvgSellingPrice : currentSellingPrice);

                    decimal spread = avgSell - avgImp;
                    decimal margin = avgSell > 0 ? Math.Round(spread / avgSell * 100, 1) : 0;

                    timelinePoints.Add(new PriceVolatilityPointDto
                    {
                        Label = label,
                        AvgImportPrice = avgImp,
                        AvgSellingPrice = avgSell,
                        Spread = spread,
                        MarginPercent = margin
                    });

                    weekStart = weekStart.AddDays(7);
                }
            }
            else if (tf == "year")
            {
                var cur = new DateTime(from.Year, 1, 1);
                while (cur <= to.Date)
                {
                    int q = (cur.Month - 1) / 3 + 1;
                    var qEnd = cur.AddMonths(3).AddTicks(-1);
                    string label = $"Q{q}/{cur.Year}";

                    var qImports = poDetails
                        .Where(p => p.PurchaseOrder.OrderDate >= cur && p.PurchaseOrder.OrderDate <= qEnd)
                        .Select(p => Normalize(p.UoMId, p.UnitPrice, p.OrderQuantity))
                        .ToList();

                    var qSales = orderDetails
                        .Where(o => o.Order.OrderDate >= cur && o.Order.OrderDate <= qEnd)
                        .Select(o => Normalize(o.UoMId, o.UnitPrice, o.Quantity))
                        .ToList();

                    decimal avgImp = qImports.Count > 0 && qImports.Sum(x => x.normQty) > 0
                        ? Math.Round(qImports.Sum(x => x.normPrice * x.normQty) / qImports.Sum(x => x.normQty), 0)
                        : (timelinePoints.Count > 0 ? timelinePoints.Last().AvgImportPrice : latestImportPrice);

                    decimal avgSell = qSales.Count > 0 && qSales.Sum(x => x.normQty) > 0
                        ? Math.Round(qSales.Sum(x => x.normPrice * x.normQty) / qSales.Sum(x => x.normQty), 0)
                        : (timelinePoints.Count > 0 ? timelinePoints.Last().AvgSellingPrice : currentSellingPrice);

                    decimal spread = avgSell - avgImp;
                    decimal margin = avgSell > 0 ? Math.Round(spread / avgSell * 100, 1) : 0;

                    timelinePoints.Add(new PriceVolatilityPointDto
                    {
                        Label = label,
                        AvgImportPrice = avgImp,
                        AvgSellingPrice = avgSell,
                        Spread = spread,
                        MarginPercent = margin
                    });

                    cur = cur.AddMonths(3);
                }
            }
            else
            {
                // Month
                var cur = new DateTime(from.Year, from.Month, 1);
                while (cur <= to.Date)
                {
                    var mEnd = cur.AddMonths(1).AddTicks(-1);
                    string label = $"T{cur.Month:D2}/{cur.Year}";

                    var mImports = poDetails
                        .Where(p => p.PurchaseOrder.OrderDate >= cur && p.PurchaseOrder.OrderDate <= mEnd)
                        .Select(p => Normalize(p.UoMId, p.UnitPrice, p.OrderQuantity))
                        .ToList();

                    var mSales = orderDetails
                        .Where(o => o.Order.OrderDate >= cur && o.Order.OrderDate <= mEnd)
                        .Select(o => Normalize(o.UoMId, o.UnitPrice, o.Quantity))
                        .ToList();

                    decimal avgImp = mImports.Count > 0 && mImports.Sum(x => x.normQty) > 0
                        ? Math.Round(mImports.Sum(x => x.normPrice * x.normQty) / mImports.Sum(x => x.normQty), 0)
                        : (timelinePoints.Count > 0 ? timelinePoints.Last().AvgImportPrice : latestImportPrice);

                    decimal avgSell = mSales.Count > 0 && mSales.Sum(x => x.normQty) > 0
                        ? Math.Round(mSales.Sum(x => x.normPrice * x.normQty) / mSales.Sum(x => x.normQty), 0)
                        : (timelinePoints.Count > 0 ? timelinePoints.Last().AvgSellingPrice : currentSellingPrice);

                    decimal spread = avgSell - avgImp;
                    decimal margin = avgSell > 0 ? Math.Round(spread / avgSell * 100, 1) : 0;

                    timelinePoints.Add(new PriceVolatilityPointDto
                    {
                        Label = label,
                        AvgImportPrice = avgImp,
                        AvgSellingPrice = avgSell,
                        Spread = spread,
                        MarginPercent = margin
                    });

                    cur = cur.AddMonths(1);
                }
            }

            // 8. Danh sách giao dịch chi tiết
            var transactions = new List<PriceTransactionDetailDto>();

            foreach (var po in poDetails.OrderByDescending(p => p.PurchaseOrder.OrderDate).Take(15))
            {
                var norm = Normalize(po.UoMId, po.UnitPrice, po.OrderQuantity);
                transactions.Add(new PriceTransactionDetailDto
                {
                    Date = po.PurchaseOrder.OrderDate,
                    Type = "Nhập hàng",
                    DocumentCode = po.PurchaseOrder.OrderCode,
                    PartnerName = po.PurchaseOrder.Supplier?.Name ?? "Nhà cung cấp",
                    OriginalUnitPrice = po.UnitPrice,
                    OriginalUoMName = po.UoM?.Name ?? baseUoMName,
                    NormalizedUnitPrice = norm.normPrice,
                    QuantityInBaseUoM = norm.normQty,
                    BaseUoMName = baseUoMName
                });
            }

            foreach (var ord in orderDetails.OrderByDescending(o => o.Order.OrderDate).Take(15))
            {
                var norm = Normalize(ord.UoMId, ord.UnitPrice, ord.Quantity);
                transactions.Add(new PriceTransactionDetailDto
                {
                    Date = ord.Order.OrderDate,
                    Type = "Bán hàng",
                    DocumentCode = ord.Order.OrderCode,
                    PartnerName = ord.Order.Customer?.Name ?? ord.Order.ReceiverName ?? "Khách hàng",
                    OriginalUnitPrice = ord.UnitPrice,
                    OriginalUoMName = ord.UoM?.Name ?? baseUoMName,
                    NormalizedUnitPrice = norm.normPrice,
                    QuantityInBaseUoM = norm.normQty,
                    BaseUoMName = baseUoMName
                });
            }

            var sortedTx = transactions.OrderByDescending(t => t.Date).Take(20).ToList();

            return new DashboardPriceVolatilityDto
            {
                VariantId = variant.Id,
                VariantName = variant.Name,
                VariantCode = variant.Code,
                ProductName = variant.Product?.Name ?? variant.Name,
                BaseUoMName = baseUoMName,
                LatestImportPrice = latestImportPrice,
                CurrentSellingPrice = currentSellingPrice,
                PriceSpread = priceSpread,
                MarginPercent = marginPercent,
                ImportPriceChangePercent = importChangePercent,
                Timeline = timelinePoints,
                Transactions = sortedTx
            };
        }

        private static int GetIsoWeek(DateTime date)
        {
            var day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}

