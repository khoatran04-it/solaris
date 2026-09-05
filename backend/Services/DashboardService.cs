using backend.Data;
using backend.DTOs.DashboardDTOs;
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

            var allDetails = completedOrders.SelectMany(o => o.Details).ToList();
            var totalRev = completedOrders.Sum(o => o.TotalAmount);

            // Top products
            var topProducts = allDetails
                .Where(d => d.Variant != null)
                .GroupBy(d => new { d.VariantId, d.Variant!.Name, d.Variant.Code })
                .Select(g =>
                {
                    var rev = g.Sum(d => d.UnitPrice * d.Quantity);
                    return new TopProductItem
                    {
                        VariantId = g.Key.VariantId,
                        Name = g.Key.Name,
                        Code = g.Key.Code,
                        UoM = g.FirstOrDefault()?.UoM?.Name ?? "",
                        QuantitySold = (int)g.Sum(d => d.Quantity),
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
                    occupiedCbm += (i.Variant?.UnitCbm ?? 0) * totalUnitQty;
                    occupiedKg += (i.Variant?.GrossWeightKg ?? 0) * totalUnitQty;
                }
                var cbmPercent = (w.TotalCapacityCbm ?? 0) > 0 ? Math.Round(occupiedCbm / (w.TotalCapacityCbm ?? 1) * 100, 1) : 0;
                var kgPercent = (w.MaxWeightCapacityKg ?? 0) > 0 ? Math.Round(occupiedKg / (w.MaxWeightCapacityKg ?? 1) * 100, 1) : 0;
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

            // Total stock value (Available * SellPrice)
            decimal totalValue = 0;
            foreach (var inv in inventories)
            {
                var price = prices
                    .Where(p => p.VariantId == inv.VariantId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault();
                totalValue += (price?.Price ?? 0) * inv.QuantityAvailable;
            }
            compartments.TotalValue = totalValue;

            // Top space-consuming
            var topSpace = inventories
                .Where(i => i.Variant != null && i.Variant.UnitCbm > 0)
                .GroupBy(i => new { i.VariantId, i.Variant!.Name, i.Variant.Code, i.Variant.UnitCbm })
                .Select(g =>
                {
                    var totalQty = (int)g.Sum(i => i.QuantityAvailable + i.QuantityReserved + i.QuantityQC + i.QuantityDamaged);
                    return new TopSpaceConsumingItem
                    {
                        VariantName = g.Key.Name,
                        VariantCode = g.Key.Code,
                        TotalQty = totalQty,
                        TotalCbm = Math.Round((g.Key.UnitCbm ?? 0) * totalQty, 2)
                    };
                })
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
                    .ThenInclude(v => v!.Product)
                .Include(wi => wi.Warehouse)
                .ToListAsync();

            var prices = await _db.ProductVariantPrices
                .AsNoTracking()
                .ToListAsync();

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
                    var price = prices
                        .Where(p => p.VariantId == i.VariantId)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault();
                    var unitPrice = price?.Price ?? 0;
                    return new ExpiringBatchItem
                    {
                        BatchCode = i.Batch.BatchCode,
                        ProductName = i.Variant?.Product?.Name ?? "",
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
    }
}

