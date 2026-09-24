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

        private static string NormalizeProvinceName(string? rawAddress)
        {
            if (string.IsNullOrWhiteSpace(rawAddress))
                return "Không xác định";

            var parts = rawAddress.Split(',');
            if (parts.Length == 0)
                return "Không xác định";

            var provincePart = parts[^1].Trim();
            if (string.IsNullOrWhiteSpace(provincePart))
                return "Không xác định";

            var cleaned = provincePart;
            string[] prefixes = ["Thành phố ", "thành phố ", "TP. ", "Tp. ", "TP ", "Tp ", "Tỉnh ", "tỉnh "];
            foreach (var p in prefixes)
            {
                if (cleaned.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(p.Length).Trim();
                    break;
                }
            }

            if (cleaned.Equals("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("HCM", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("Sài Gòn", StringComparison.OrdinalIgnoreCase))
                return "Hồ Chí Minh";

            if (cleaned.Equals("Hà Nội", StringComparison.OrdinalIgnoreCase) ||
                cleaned.Equals("HN", StringComparison.OrdinalIgnoreCase))
                return "Hà Nội";

            if (cleaned.Equals("Đà Nẵng", StringComparison.OrdinalIgnoreCase))
                return "Đà Nẵng";

            if (cleaned.Equals("Cần Thơ", StringComparison.OrdinalIgnoreCase))
                return "Cần Thơ";

            if (cleaned.Equals("Hải Phòng", StringComparison.OrdinalIgnoreCase))
                return "Hải Phòng";

            return cleaned;
        }

        // ============================================================
        // DASHBOARD 1: OVERVIEW
        // ============================================================
        public async Task<DashboardOverviewDto> GetOverviewAsync(string period, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null)
        {
            var (from, to) = ResolveDateRange(period, fromDate, toDate);
            var (prevFrom, prevTo) = GetPreviousPeriod(from, to);

            var ordersQuery = _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= from && o.OrderDate <= to)
                .Include(o => o.Customer)
                .AsQueryable();

            var prevOrdersQuery = _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= prevFrom && o.OrderDate <= prevTo)
                .Select(o => new { o.Status, o.TotalAmount, o.WarehouseId })
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                ordersQuery = ordersQuery.Where(o => o.WarehouseId.HasValue && allowedWarehouseIds.Contains(o.WarehouseId.Value));
                prevOrdersQuery = prevOrdersQuery.Where(o => o.WarehouseId.HasValue && allowedWarehouseIds.Contains(o.WarehouseId.Value));
            }

            var orders = await ordersQuery.ToListAsync();
            var prevOrders = await prevOrdersQuery.ToListAsync();

            var totalRevenue = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalAmount);

            var prevRevenue = prevOrders
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalAmount);

            var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
            var totalOrders = orders.Count;
            var fulfillableOrders = orders.Count(o => o.Status != OrderStatus.Cancelled);
            decimal revenueGrowth = prevRevenue == 0 ? 0
                : Math.Round((totalRevenue - prevRevenue) / prevRevenue * 100, 1);

            // Timeline - group by day/month based on span
            var span = (to - from).TotalDays;
            List<RevenueTimelinePoint> timeline;
            if (span <= 31)
            {
                var dailyCompleted = orders
                    .Where(o => o.Status == OrderStatus.Completed)
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => new { Revenue = g.Sum(o => o.TotalAmount), OrderCount = g.Count() });

                timeline = new List<RevenueTimelinePoint>();
                for (var cur = from.Date; cur <= to.Date; cur = cur.AddDays(1))
                {
                    dailyCompleted.TryGetValue(cur, out var val);
                    timeline.Add(new RevenueTimelinePoint
                    {
                        Label = cur.ToString("dd/MM"),
                        Revenue = val?.Revenue ?? 0,
                        OrderCount = val?.OrderCount ?? 0
                    });
                }
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
                AverageOrderValue = completedOrders == 0 ? 0 : Math.Round(totalRevenue / completedOrders, 0),
                FulfillmentRatePercent = fulfillableOrders == 0 ? 0 : Math.Round((decimal)completedOrders / fulfillableOrders * 100, 1),
                RevenueTimeline = timeline,
                PaymentMethodBreakdown = paymentBreakdown,
                OrderStatusPipeline = statusPipeline,
                RecentOrders = recentOrders
            };
        }

        // ============================================================
        // DASHBOARD 2: SALES & GEOGRAPHY
        // ============================================================
        public async Task<DashboardSalesGeographyDto> GetSalesGeographyAsync(string period, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null)
        {
            var (from, to) = ResolveDateRange(period, fromDate, toDate);

            var ordersQuery = _db.Orders
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
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                ordersQuery = ordersQuery.Where(o => o.WarehouseId.HasValue && allowedWarehouseIds.Contains(o.WarehouseId.Value));
            }

            var completedOrders = await ordersQuery.ToListAsync();

            var uomMap = await _db.UoMs.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Name);
            var allDetails = completedOrders.SelectMany(o => o.Details).ToList();
            var totalRev = completedOrders.Sum(o => o.TotalAmount);
            var totalDetailRev = allDetails.Sum(d => d.UnitPrice * d.Quantity - d.DiscountAmount);
            if (totalDetailRev <= 0) totalDetailRev = totalRev;

            // Top products (quy đổi về Base UoM và trừ chiết khấu dòng hàng)
            var topProducts = allDetails
                .Where(d => d.Variant != null)
                .GroupBy(d => new { d.VariantId, d.Variant!.Name, d.Variant.Code })
                .Select(g =>
                {
                    var rev = g.Sum(d => d.UnitPrice * d.Quantity - d.DiscountAmount);
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
                        RevenuePercent = totalDetailRev == 0 ? 0 : Math.Round(rev / totalDetailRev * 100, 1)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            // Category breakdown (chuẩn hóa Base UoM và trừ chiết khấu)
            var categoryBreakdown = allDetails
                .Where(d => d.Variant?.Product?.Category?.CategoryGroup != null)
                .GroupBy(d => d.Variant!.Product!.Category!.CategoryGroup!.Name)
                .Select(g =>
                {
                    var rev = g.Sum(d => d.UnitPrice * d.Quantity - d.DiscountAmount);
                    return new CategoryBreakdownItem
                    {
                        CategoryGroupName = g.Key,
                        TotalRevenue = rev,
                        QuantitySold = (int)g.Sum(d => d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity),
                        Percent = totalDetailRev == 0 ? 0 : Math.Round(rev / totalDetailRev * 100, 1)
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            // Geography breakdown - from snapshot DeliveryAddress with province normalization
            var geographyBreakdown = completedOrders
                .GroupBy(o => NormalizeProvinceName(o.DeliveryAddress))
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
        public async Task<DashboardInventoryCapacityDto> GetInventoryCapacityAsync(List<int>? allowedWarehouseIds = null)
        {
            var warehousesQuery = _db.Warehouses
                .AsNoTracking()
                .Where(w => !w.IsDeleted && w.IsActive)
                .AsQueryable();

            var inventoriesQuery = _db.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Variant)
                .Include(wi => wi.Warehouse)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                warehousesQuery = warehousesQuery.Where(w => allowedWarehouseIds.Contains(w.Id));
                inventoriesQuery = inventoriesQuery.Where(wi => allowedWarehouseIds.Contains(wi.WarehouseId));
            }

            var warehouses = await warehousesQuery.ToListAsync();
            var inventories = await inventoriesQuery.ToListAsync();

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

            // Compartments (Lưu giữ số lượng decimal chuẩn cho nông sản cân ký)
            var compartments = new InventoryCompartmentsDto
            {
                AvailableQty = inventories.Sum(i => i.QuantityAvailable),
                ReservedQty = inventories.Sum(i => i.QuantityReserved),
                InQcQty = inventories.Sum(i => i.QuantityQC),
                DamagedQty = inventories.Sum(i => i.QuantityDamaged)
            };

            // Lấy giá vốn từ PO gần nhất hoặc Giá mua của NCC
            var latestCosts = await _db.PurchaseOrderDetails
                .AsNoTracking()
                .Where(pod => pod.PurchaseOrder != null && !pod.PurchaseOrder.IsDeleted && pod.UnitPrice > 0)
                .OrderByDescending(pod => pod.PurchaseOrder!.OrderDate)
                .GroupBy(pod => pod.VariantId)
                .Select(g => new { VariantId = g.Key, UnitPrice = g.First().UnitPrice })
                .ToDictionaryAsync(x => x.VariantId, x => x.UnitPrice);

            var supplierCosts = await _db.SupplierProducts
                .AsNoTracking()
                .Where(sp => !sp.IsDeleted && sp.IsActive && sp.LastImportPrice > 0)
                .GroupBy(sp => sp.VariantId)
                .Select(g => new { VariantId = g.Key, LastImportPrice = g.First().LastImportPrice })
                .ToDictionaryAsync(x => x.VariantId, x => x.LastImportPrice);

            // Định giá tài sản tồn kho theo Giá Vốn (COGS): bao gồm Available, Reserved và QC
            decimal totalStockCostValue = 0;
            foreach (var inv in inventories)
            {
                decimal unitCost = 0;
                if (latestCosts.TryGetValue(inv.VariantId, out var poC) && poC > 0)
                    unitCost = poC;
                else if (supplierCosts.TryGetValue(inv.VariantId, out var spC) && spC > 0)
                    unitCost = spC;
                else
                {
                    // Fallback theo 85% giá bán niêm yết
                    var pr = prices.Where(p => p.VariantId == inv.VariantId).OrderByDescending(p => p.CreatedAt).FirstOrDefault();
                    unitCost = (pr?.Price ?? 50000m) * 0.85m;
                }

                // Tài sản lưu kho thực tế bao gồm hàng khả dụng, hàng đã giữ chỗ đơn bán, và hàng đang kiểm định QC
                decimal totalAssetQty = inv.QuantityAvailable + inv.QuantityReserved + inv.QuantityQC;
                totalStockCostValue += totalAssetQty * unitCost;
            }
            compartments.TotalValue = Math.Round(totalStockCostValue, 0);

            // Top space-consuming
            var topSpace = inventories
                .Where(i => i.Variant != null)
                .GroupBy(i => new { i.VariantId, i.Variant!.Name, i.Variant.Code })
                .Select(g =>
                {
                    var variant = g.First().Variant;
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

            // Cảnh báo hết hàng (Out of stock) & tồn kho dưới định mức (Low stock)
            var lowStockCandidates = inventories
                .Where(i => i.Variant != null && i.Variant.InventoryGuideline > 0 && i.QuantityAvailable < i.Variant.InventoryGuideline)
                .Select(i =>
                {
                    decimal available = i.QuantityAvailable;
                    decimal guideline = i.Variant!.InventoryGuideline;
                    decimal shortfall = Math.Max(0, guideline - available);
                    bool isOos = available <= 0;

                    return new LowStockAlertItem
                    {
                        VariantName = i.Variant!.Name,
                        VariantCode = i.Variant.Code,
                        WarehouseName = i.Warehouse?.Name ?? "",
                        AvailableQty = available,
                        GuidelineQty = guideline,
                        ShortfallQty = shortfall,
                        IsOutOfStock = isOos
                    };
                })
                .OrderBy(x => x.AvailableQty)
                .ToList();

            int totalOutOfStockCount = lowStockCandidates.Count(x => x.IsOutOfStock);
            int totalLowStockCount = lowStockCandidates.Count;
            var lowStock = lowStockCandidates.Take(10).ToList();

            return new DashboardInventoryCapacityDto
            {
                TotalStockValue = Math.Round(totalStockCostValue, 0),
                TotalActiveWarehouses = warehouses.Count,
                TotalLowStockCount = totalLowStockCount,
                TotalOutOfStockCount = totalOutOfStockCount,
                WarehouseCapacities = warehouseCapacities,
                InventoryCompartments = compartments,
                TopSpaceConsumingProducts = topSpace,
                LowStockAlerts = lowStock
            };
        }

        // ============================================================
        // DASHBOARD 4: QUALITY & EXPIRY
        // ============================================================
        public async Task<DashboardQualityExpiryDto> GetQualityExpiryAsync(List<int>? allowedWarehouseIds = null)
        {
            var today = DateTime.UtcNow.Date;

            // Inventory with batch info (for expiry)
            var inventoriesQuery = _db.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Batch)
                .Include(wi => wi.Variant)
                .Include(wi => wi.Warehouse)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                inventoriesQuery = inventoriesQuery.Where(wi => allowedWarehouseIds.Contains(wi.WarehouseId));
            }

            var inventories = await inventoriesQuery.ToListAsync();

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

            // Expiry overview (đếm chính xác số Lô hàng độc lập - Distinct BatchId)
            var expired = activeBatches.Where(i => i.Batch!.ExpiryDate.Date < today).Select(i => i.BatchId).Distinct().Count();
            var critical = activeBatches.Where(i => i.Batch!.ExpiryDate.Date >= today && (i.Batch.ExpiryDate.Date - today).TotalDays < 3).Select(i => i.BatchId).Distinct().Count();
            var warning = activeBatches.Where(i => i.Batch!.ExpiryDate.Date >= today && (i.Batch.ExpiryDate.Date - today).TotalDays >= 3 && (i.Batch.ExpiryDate.Date - today).TotalDays < 7).Select(i => i.BatchId).Distinct().Count();
            var safe = activeBatches.Where(i => i.Batch!.ExpiryDate.Date >= today && (i.Batch.ExpiryDate.Date - today).TotalDays >= 7).Select(i => i.BatchId).Distinct().Count();

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
                    bool isExpired = daysRemaining < 0;
                    decimal totalRiskQty = i.QuantityAvailable + i.QuantityQC;
                    return new ExpiringBatchItem
                    {
                        BatchCode = i.Batch.BatchCode,
                        ProductName = productName,
                        VariantName = i.Variant?.Name ?? "",
                        WarehouseName = i.Warehouse?.Name ?? "",
                        ExpiryDate = i.Batch.ExpiryDate,
                        DaysRemaining = daysRemaining,
                        QuantityAvailable = i.QuantityAvailable,
                        QuantityQC = i.QuantityQC,
                        IsExpired = isExpired,
                        UnitPrice = unitPrice,
                        EstimatedLossValue = Math.Round(unitPrice * totalRiskQty, 0)
                    };
                })
                .Take(20)
                .ToList();

            // Inbound QC reject rate (từ InventoryReceiptDetails có lọc Soft-Delete và phân quyền Kho)
            List<InventoryReceiptDetail> receiptDetails;
            int totalOrders;
            int totalReturns;

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                receiptDetails = await _db.InventoryReceiptDetails
                    .AsNoTracking()
                    .Where(d => _db.InventoryReceipts.Any(r => r.Id == d.InventoryReceiptId && !r.IsDeleted && allowedWarehouseIds.Contains(r.WarehouseId)))
                    .ToListAsync();

                totalOrders = await _db.Orders.AsNoTracking().CountAsync(o => !o.IsDeleted && o.Status != OrderStatus.Cancelled && o.WarehouseId.HasValue && allowedWarehouseIds.Contains(o.WarehouseId.Value));
                totalReturns = await _db.CustomerReturns.AsNoTracking().CountAsync(r => !r.IsDeleted && allowedWarehouseIds.Contains(r.WarehouseId));
            }
            else
            {
                receiptDetails = await _db.InventoryReceiptDetails
                    .AsNoTracking()
                    .Where(d => d.InventoryReceiptId == 0 || _db.InventoryReceipts.Any(r => r.Id == d.InventoryReceiptId && !r.IsDeleted))
                    .ToListAsync();

                totalOrders = await _db.Orders.AsNoTracking().CountAsync(o => !o.IsDeleted && o.Status != OrderStatus.Cancelled);
                totalReturns = await _db.CustomerReturns.AsNoTracking().CountAsync(r => !r.IsDeleted);
            }

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
        public async Task<DashboardFinancialPerformanceDto> GetFinancialPerformanceAsync(string period, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null)
        {
            var (from, to) = ResolveDateRange(period, fromDate, toDate);
            var (prevFrom, prevTo) = GetPreviousPeriod(from, to);

            // 1. Đơn hàng bán trong kỳ
            var ordersQuery = _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= from && o.OrderDate <= to)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
                                .ThenInclude(c => c!.CategoryGroup)
                .AsQueryable();

            // Đơn hàng bán kỳ trước (để tính tăng trưởng)
            var prevOrdersQuery = _db.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.Status == OrderStatus.Completed && o.OrderDate >= prevFrom && o.OrderDate <= prevTo)
                .AsQueryable();

            // 2. Đơn trả hàng (RMA) trong kỳ & kỳ trước
            var returnsQuery = _db.CustomerReturns
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.ReturnDate >= from && r.ReturnDate <= to)
                .Include(r => r.Details)
                    .ThenInclude(d => d.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Category)
                                .ThenInclude(c => c!.CategoryGroup)
                .AsQueryable();

            var prevReturnsQuery = _db.CustomerReturns
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.Status == CustomerReturnStatus.Completed && r.ReturnDate >= prevFrom && r.ReturnDate <= prevTo)
                .AsQueryable();

            // 3. Đơn mua hàng PO trong kỳ
            var poQuery = _db.PurchaseOrders
                .AsNoTracking()
                .Where(po => !po.IsDeleted && po.OrderDate >= from && po.OrderDate <= to)
                .Include(po => po.Supplier)
                .Include(po => po.Details)
                .AsQueryable();

            // 4. Phiếu nhập kho trong kỳ
            var receiptsQuery = _db.InventoryReceipts
                .AsNoTracking()
                .Where(ir => !ir.IsDeleted && ir.CreatedAt >= from && ir.CreatedAt <= to)
                .Include(ir => ir.Details)
                .Include(ir => ir.Supplier)
                .AsQueryable();

            var inventoriesQuery = _db.WarehouseInventories
                .AsNoTracking()
                .Include(wi => wi.Batch)
                .AsQueryable();

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                ordersQuery = ordersQuery.Where(o => o.WarehouseId.HasValue && allowedWarehouseIds.Contains(o.WarehouseId.Value));
                prevOrdersQuery = prevOrdersQuery.Where(o => o.WarehouseId.HasValue && allowedWarehouseIds.Contains(o.WarehouseId.Value));
                returnsQuery = returnsQuery.Where(r => allowedWarehouseIds.Contains(r.WarehouseId));
                prevReturnsQuery = prevReturnsQuery.Where(r => allowedWarehouseIds.Contains(r.WarehouseId));
                poQuery = poQuery.Where(po => po.WarehouseId.HasValue && allowedWarehouseIds.Contains(po.WarehouseId.Value));
                receiptsQuery = receiptsQuery.Where(ir => allowedWarehouseIds.Contains(ir.WarehouseId));
                inventoriesQuery = inventoriesQuery.Where(wi => allowedWarehouseIds.Contains(wi.WarehouseId));
            }

            var orders = await ordersQuery.ToListAsync();
            var prevCompletedOrders = await prevOrdersQuery.ToListAsync();
            var returns = await returnsQuery.ToListAsync();
            var prevReturns = await prevReturnsQuery.ToListAsync();
            var purchaseOrders = await poQuery.ToListAsync();
            var receipts = await receiptsQuery.ToListAsync();
            var inventories = await inventoriesQuery.ToListAsync();

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

            // --- TÍNH TOÁN CHỈ SỐ DOANH THU & GIÁ VỐN ---
            var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
            var completedReturns = returns.Where(r => r.Status == CustomerReturnStatus.Completed).ToList();

            decimal grossRevenue = completedOrders.Sum(o => o.TotalAmount);
            decimal customerRefunds = completedReturns.Sum(r => r.RefundAmount);
            decimal netRevenue = Math.Max(0, grossRevenue - customerRefunds);

            decimal prevGrossRev = prevCompletedOrders.Sum(o => o.TotalAmount);
            decimal prevRefunds = prevReturns.Sum(r => r.RefundAmount);
            decimal prevNetRev = Math.Max(0, prevGrossRev - prevRefunds);
            decimal netRevenueGrowth = prevNetRev == 0 ? 0 : Math.Round((netRevenue - prevNetRev) / prevNetRev * 100, 1);

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
                        unitCost = d.UnitPrice * 0.85m; // Fallback 85% giá bán (chuẩn ngành nông sản tươi sống biên lợi nhuận gộp 15%)

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

            // Hoàn nhập giá vốn hàng bán (COGS Reversal) theo chuẩn mực kế toán VAS 14 / TT 200:
            // Khi phát sinh hàng bán bị trả lại, ngoài việc giảm trừ Doanh thu, bắt buộc phải hoàn nhập Giá vốn tương ứng (Ghi Nợ TK 156 / Có TK 632).
            // CHUẨN MỰC: Chỉ hoàn nhập giá vốn cho số lượng thực tế đạt phẩm cấp tái nhập kho (AcceptedQuantity).
            // Hàng lỗi hỏng (DamagedQuantity) được hạch toán riêng vào Chi phí hao hụt (Shrinkage Loss), không được tính hoàn nhập giá vốn.
            decimal totalReturnedCogs = 0;
            foreach (var ret in completedReturns)
            {
                foreach (var d in ret.Details)
                {
                    decimal unitCost = 0;
                    if (latestPoPrices.TryGetValue(d.VariantId, out var poCost) && poCost > 0)
                        unitCost = poCost;
                    else if (supplierProductPrices.TryGetValue(d.VariantId, out var spCost) && spCost > 0)
                        unitCost = spCost;
                    else
                        unitCost = d.UnitPrice * 0.85m;

                    decimal retQty = d.AcceptedQuantity;
                    decimal lineReturnedCogs = unitCost * retQty;
                    totalReturnedCogs += lineReturnedCogs;

                    var catName = d.Variant?.Product?.Category?.CategoryGroup?.Name ?? "Nông sản tổng hợp";
                    if (categoryMap.ContainsKey(catName))
                    {
                        var current = categoryMap[catName];
                        decimal adjustedCogs = Math.Max(0, current.Cogs - lineReturnedCogs);
                        decimal adjustedRev = Math.Max(0, current.Revenue - d.RefundAmount);
                        int adjustedQty = Math.Max(0, current.Qty - (int)retQty);
                        categoryMap[catName] = (adjustedRev, adjustedCogs, adjustedQty);
                    }
                }
            }

            decimal netCogs = Math.Max(0, totalCogs - totalReturnedCogs);
            decimal grossProfit = netRevenue - netCogs;
            decimal grossMarginPercent = netRevenue == 0 ? 0 : Math.Round(grossProfit / netRevenue * 100, 1);

            // --- TÍNH TOÁN CẦU NỐI DÒNG TIỀN (CASH FLOW BRIDGE) ---
            decimal onlineInflow = orders
                .Where(o => (o.PaymentStatus == PaymentStatus.Paid || o.PaymentStatus == PaymentStatus.PartiallyRefunded || o.PaymentStatus == PaymentStatus.Refunded) &&
                            o.Status != OrderStatus.Cancelled &&
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
                    var recVal = g.Sum(p => p.SettledAmount ?? (p.Details.Any(d => d.ReceivedQuantity > 0)
                        ? p.Details.Sum(d => d.ReceivedQuantity * d.UnitPrice)
                        : (p.Status == PurchaseOrderStatus.Completed ? p.TotalAmount : 0m)));

                    var qcRejVal = g.Sum(p => p.Details.Sum(d => d.RejectedQuantity * d.UnitPrice));
                    if (qcRejVal == 0)
                    {
                        var supRecs = receipts.Where(r => r.SupplierId == g.Key.SupplierId && r.Status == InventoryReceiptStatus.Completed);
                        qcRejVal = supRecs.SelectMany(r => r.Details).Sum(d => d.RejectedQuantity * (latestPoPrices.TryGetValue(d.VariantId, out var c) ? c : 50000m));
                    }

                    var pending = g.Where(p => p.Status == PurchaseOrderStatus.Approved || p.Status == PurchaseOrderStatus.PartiallyReceived)
                        .Sum(p => Math.Max(0, p.TotalAmount - p.Details.Sum(d => (d.ReceivedQuantity + d.RejectedQuantity) * d.UnitPrice)));

                    var paidVal = g.Sum(p => p.PaidAmount);
                    var remainingDebt = Math.Max(0, recVal - paidVal);

                    string paymentStatusText;
                    if (recVal == 0)
                        paymentStatusText = "Không phát sinh nợ";
                    else if (remainingDebt == 0)
                        paymentStatusText = "Đã tất toán";
                    else if (paidVal > 0)
                        paymentStatusText = "Đã trả một phần";
                    else
                        paymentStatusText = "Chưa thanh toán";

                    return new SupplierPayableItem
                    {
                        SupplierId = g.Key.SupplierId,
                        SupplierName = g.Key.Name,
                        SupplierCode = g.Key.Code,
                        TotalPoCount = poCount,
                        TotalPoValue = poVal,
                        ReceivedValue = recVal,
                        QcRejectedValue = qcRejVal,
                        TotalPaidAmount = paidVal,
                        RemainingDebt = remainingDebt,
                        PaymentStatus = paymentStatusText,
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
                    TotalPaidAmount = 0,
                    RemainingDebt = 0,
                    PaymentStatus = "Không phát sinh nợ",
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
                    cost = 50000;

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

            // --- TIMELINE: DOANH THU VS GIÁ VỐN VS LỢI NHUẬN (LÀM MƯỢT VỚI ZERO-DAY FILLING) ---
            var span = (to - from).TotalDays;
            List<FinancialTimelinePoint> timeline;
            if (span <= 31)
            {
                var dayOrderGroup = completedOrders
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());

                timeline = new List<FinancialTimelinePoint>();
                for (var cur = from.Date; cur <= to.Date; cur = cur.AddDays(1))
                {
                    dayOrderGroup.TryGetValue(cur, out var dayOrders);
                    var rev = dayOrders?.Sum(o => o.TotalAmount) ?? 0;
                    decimal dayCogs = 0;
                    if (dayOrders != null)
                    {
                        foreach (var o in dayOrders)
                        {
                            foreach (var d in o.Details)
                            {
                                decimal c = latestPoPrices.GetValueOrDefault(d.VariantId, supplierProductPrices.GetValueOrDefault(d.VariantId, d.UnitPrice * 0.85m));
                                dayCogs += c * (d.BaseQuantity > 0 ? d.BaseQuantity : d.Quantity);
                            }
                        }
                    }

                    // Hoàn nhập trả hàng trong ngày
                    var dayReturns = completedReturns.Where(r => r.ReturnDate.Date == cur).ToList();
                    var dayRefund = dayReturns.Sum(r => r.RefundAmount);
                    decimal dayReturnedCogs = 0;
                    foreach (var ret in dayReturns)
                    {
                        foreach (var d in ret.Details)
                        {
                            decimal c = latestPoPrices.GetValueOrDefault(d.VariantId, supplierProductPrices.GetValueOrDefault(d.VariantId, d.UnitPrice * 0.85m));
                            decimal retQty = d.AcceptedQuantity; // Chuẩn VAS 14: Chỉ hoàn nhập giá vốn cho hàng đạt chuẩn tái nhập kho
                            dayReturnedCogs += c * retQty;
                        }
                    }

                    decimal netDayRev = Math.Max(0, rev - dayRefund);
                    decimal netDayCogs = Math.Max(0, dayCogs - dayReturnedCogs);

                    timeline.Add(new FinancialTimelinePoint
                    {
                        Label = cur.ToString("dd/MM"),
                        Revenue = netDayRev,
                        Cogs = netDayCogs,
                        GrossProfit = netDayRev - netDayCogs,
                        CashInflow = netDayRev,
                        CashOutflow = netDayCogs
                    });
                }
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

                        // Hoàn nhập trả hàng trong tháng
                        var monthReturns = completedReturns.Where(r => r.ReturnDate.Year == g.Key.Year && r.ReturnDate.Month == g.Key.Month).ToList();
                        var monthRefund = monthReturns.Sum(r => r.RefundAmount);
                        decimal monthReturnedCogs = 0;
                        foreach (var ret in monthReturns)
                        {
                            foreach (var d in ret.Details)
                            {
                                decimal c = latestPoPrices.GetValueOrDefault(d.VariantId, supplierProductPrices.GetValueOrDefault(d.VariantId, d.UnitPrice * 0.7m));
                                decimal retQty = d.AcceptedQuantity; // Chuẩn VAS 14: Chỉ hoàn nhập giá vốn cho hàng đạt chuẩn tái nhập kho
                                monthReturnedCogs += c * retQty;
                            }
                        }

                        decimal netMonthRev = Math.Max(0, rev - monthRefund);
                        decimal netMonthCogs = Math.Max(0, monthCogs - monthReturnedCogs);

                        return new FinancialTimelinePoint
                        {
                            Label = $"T{g.Key.Month}/{g.Key.Year % 100}",
                            Revenue = netMonthRev,
                            Cogs = netMonthCogs,
                            GrossProfit = netMonthRev - netMonthCogs,
                            CashInflow = netMonthRev,
                            CashOutflow = netMonthCogs
                        };
                    }).ToList();
            }

            return new DashboardFinancialPerformanceDto
            {
                GrossRevenue = grossRevenue,
                CustomerRefunds = customerRefunds,
                NetRevenue = netRevenue,
                TotalCogs = netCogs,
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

        public async Task<List<SkuSelectItemDto>> GetPriceVolatilitySkusAsync(List<int>? allowedWarehouseIds = null)
        {
            var query = _db.ProductVariants
                .AsNoTracking()
                .Where(v => !v.IsDeleted && v.IsActive);

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                var warehouseVariantIds = await _db.WarehouseInventories
                    .Where(il => allowedWarehouseIds.Contains(il.WarehouseId))
                    .Select(il => il.VariantId)
                    .Distinct()
                    .ToListAsync();

                if (warehouseVariantIds.Any())
                {
                    query = query.Where(v => warehouseVariantIds.Contains(v.Id));
                }
            }

            var variants = await query
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

        public async Task<DashboardPriceVolatilityDto> GetPriceVolatilityAsync(int? variantId, string timeframe, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null)
        {
            // 1. Xác định SKU cần xem
            int targetVariantId = 0;
            if (variantId.HasValue && variantId.Value > 0)
            {
                targetVariantId = variantId.Value;
            }
            else
            {
                var popOrderQuery = _db.OrderDetails
                    .Where(od => !od.Order.IsDeleted && od.Order.Status != OrderStatus.Cancelled);

                if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
                {
                    popOrderQuery = popOrderQuery.Where(od => od.Order.WarehouseId.HasValue && allowedWarehouseIds.Contains(od.Order.WarehouseId.Value));
                }

                var popularVariantId = await popOrderQuery
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
                    var firstVarQuery = _db.ProductVariants
                        .Where(v => !v.IsDeleted && v.IsActive);

                    if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
                    {
                        var whVarIds = _db.WarehouseInventories
                            .Where(il => allowedWarehouseIds.Contains(il.WarehouseId))
                            .Select(il => il.VariantId);
                        if (await whVarIds.AnyAsync())
                        {
                            firstVarQuery = firstVarQuery.Where(v => whVarIds.Contains(v.Id));
                        }
                    }

                    targetVariantId = await firstVarQuery
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

            // 4. Lấy dữ liệu Đơn mua hàng (PO - Nhập hàng) có lọc theo kho
            var poQuery = _db.PurchaseOrderDetails
                .AsNoTracking()
                .Where(pod => pod.VariantId == targetVariantId &&
                              pod.PurchaseOrder != null &&
                              !pod.PurchaseOrder.IsDeleted &&
                              pod.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled &&
                              pod.PurchaseOrder.Status != PurchaseOrderStatus.Draft &&
                              pod.PurchaseOrder.OrderDate >= from &&
                              pod.PurchaseOrder.OrderDate <= to);

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                poQuery = poQuery.Where(pod => pod.PurchaseOrder != null && pod.PurchaseOrder.WarehouseId.HasValue && allowedWarehouseIds.Contains(pod.PurchaseOrder.WarehouseId.Value));
            }

            var poDetails = await poQuery
                .Include(pod => pod.PurchaseOrder)
                    .ThenInclude(po => po!.Supplier)
                .Include(pod => pod.UoM)
                .OrderBy(pod => pod.PurchaseOrder!.OrderDate)
                .ToListAsync();

            // 5. Lấy dữ liệu Đơn bán hàng (Order - Bán hàng) có lọc theo kho
            var orderQuery = _db.OrderDetails
                .AsNoTracking()
                .Where(od => od.VariantId == targetVariantId &&
                             od.Order != null &&
                             !od.Order.IsDeleted &&
                             od.Order.Status != OrderStatus.Cancelled &&
                             od.Order.OrderDate >= from &&
                             od.Order.OrderDate <= to);

            if (allowedWarehouseIds != null && allowedWarehouseIds.Any())
            {
                orderQuery = orderQuery.Where(od => od.Order != null && od.Order.WarehouseId.HasValue && allowedWarehouseIds.Contains(od.Order.WarehouseId.Value));
            }

            var orderDetails = await orderQuery
                .Include(od => od.Order)
                    .ThenInclude(o => o.Customer)
                .Include(od => od.UoM)
                .OrderBy(od => od.Order.OrderDate)
                .ToListAsync();

            // 5b. Lấy dữ liệu Lịch sử điều chỉnh giá NCC (SupplierProductPriceHistories)
            var priceHistories = await _db.SupplierProductPriceHistories
                .AsNoTracking()
                .Where(h => h.VariantId == targetVariantId &&
                            h.EffectiveDate >= from &&
                            h.EffectiveDate <= to)
                .Include(h => h.Supplier)
                .Include(h => h.PurchaseUoM)
                .OrderBy(h => h.EffectiveDate)
                .ToListAsync();

            // 6. Tính thẻ tóm tắt (Latest Import Price, Selling Price, Margins)
            decimal latestImportPrice = 0;
            decimal prevImportPrice = 0;
            DateTime latestImportDate = DateTime.MinValue;

            // Kiểm tra từ PO
            if (poDetails.Count > 0)
            {
                var latestPo = poDetails.Last();
                var normLatest = Normalize(latestPo.UoMId, latestPo.UnitPrice, latestPo.OrderQuantity);
                latestImportPrice = normLatest.normPrice;
                latestImportDate = latestPo.PurchaseOrder.OrderDate;

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

            // Kiểm tra từ Lịch sử điều chỉnh giá NCC (nếu có bản ghi mới hơn PO hoặc không có PO)
            if (priceHistories.Count > 0)
            {
                var latestHist = priceHistories.Last();
                if (latestHist.EffectiveDate >= latestImportDate)
                {
                    var normLatestHist = Normalize(latestHist.PurchaseUoMId, latestHist.NewPrice, 1);
                    var normPrevHist = Normalize(latestHist.PurchaseUoMId, latestHist.OldPrice, 1);

                    latestImportPrice = normLatestHist.normPrice;
                    prevImportPrice = normPrevHist.normPrice > 0 ? normPrevHist.normPrice : latestImportPrice;
                    latestImportDate = latestHist.EffectiveDate;
                }
            }

            // Fallback nếu cả 2 đều không có trong khoảng thời gian
            if (latestImportPrice == 0)
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

            // Tính toán Chỉ số Biến động Giá Thực thụ (Standard Deviation & Coefficient of Variation - CV%)
            var normalizedImportPrices = new List<decimal>();
            foreach (var po in poDetails)
            {
                var n = Normalize(po.UoMId, po.UnitPrice, po.OrderQuantity);
                if (n.normPrice > 0) normalizedImportPrices.Add(n.normPrice);
            }
            foreach (var h in priceHistories)
            {
                var n = Normalize(h.PurchaseUoMId, h.NewPrice, 1);
                if (n.normPrice > 0) normalizedImportPrices.Add(n.normPrice);
            }

            decimal minImportPrice = latestImportPrice;
            decimal maxImportPrice = latestImportPrice;
            decimal cvPercent = 0;
            string volatilityLevel = "Ổn định";

            if (normalizedImportPrices.Count > 0)
            {
                minImportPrice = normalizedImportPrices.Min();
                maxImportPrice = normalizedImportPrices.Max();
                decimal mean = normalizedImportPrices.Average();
                if (mean > 0 && normalizedImportPrices.Count > 1)
                {
                    double variance = normalizedImportPrices.Average(p => Math.Pow((double)(p - mean), 2));
                    double stdDev = Math.Sqrt(variance);
                    cvPercent = Math.Round((decimal)(stdDev / (double)mean) * 100, 1);
                }

                if (cvPercent > 15)
                    volatilityLevel = "Biến động mạnh";
                else if (cvPercent >= 5)
                    volatilityLevel = "Vừa phải";
                else
                    volatilityLevel = "Ổn định";
            }

            // 7. Nhóm mốc thời gian (Timeline Points)
            var timelinePoints = new List<PriceVolatilityPointDto>();
            var tf = timeframe?.ToLower() ?? "month";

            if (tf == "week")
            {
                int diff = (7 + (int)from.Date.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                var weekStart = from.Date.AddDays(-diff);
                while (weekStart <= to.Date)
                {
                    var weekEnd = weekStart.AddDays(7).AddTicks(-1);
                    string label = $"T{GetIsoWeek(weekStart)} ({weekStart:dd/MM})";

                    var weekImports = poDetails
                        .Where(p => p.PurchaseOrder.OrderDate >= weekStart && p.PurchaseOrder.OrderDate <= weekEnd)
                        .Select(p => Normalize(p.UoMId, p.UnitPrice, p.OrderQuantity))
                        .ToList();

                    var weekPriceHistories = priceHistories
                        .Where(h => h.EffectiveDate >= weekStart && h.EffectiveDate <= weekEnd)
                        .Select(h => Normalize(h.PurchaseUoMId, h.NewPrice, 1))
                        .ToList();

                    var weekSales = orderDetails
                        .Where(o => o.Order.OrderDate >= weekStart && o.Order.OrderDate <= weekEnd)
                        .Select(o => Normalize(o.UoMId, o.UnitPrice, o.Quantity))
                        .ToList();

                    decimal weekImpQty = weekImports.Sum(x => x.normQty);
                    decimal avgImp;
                    if (weekImpQty > 0)
                    {
                        avgImp = Math.Round(weekImports.Sum(x => x.normPrice * x.normQty) / weekImpQty, 0);
                    }
                    else if (weekPriceHistories.Count > 0)
                    {
                        avgImp = Math.Round(weekPriceHistories.Last().normPrice, 0);
                    }
                    else
                    {
                        avgImp = timelinePoints.Count > 0 ? timelinePoints.Last().AvgImportPrice : latestImportPrice;
                    }

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

                    var qPriceHistories = priceHistories
                        .Where(h => h.EffectiveDate >= cur && h.EffectiveDate <= qEnd)
                        .Select(h => Normalize(h.PurchaseUoMId, h.NewPrice, 1))
                        .ToList();

                    var qSales = orderDetails
                        .Where(o => o.Order.OrderDate >= cur && o.Order.OrderDate <= qEnd)
                        .Select(o => Normalize(o.UoMId, o.UnitPrice, o.Quantity))
                        .ToList();

                    decimal qImpQty = qImports.Sum(x => x.normQty);
                    decimal avgImp;
                    if (qImpQty > 0)
                    {
                        avgImp = Math.Round(qImports.Sum(x => x.normPrice * x.normQty) / qImpQty, 0);
                    }
                    else if (qPriceHistories.Count > 0)
                    {
                        avgImp = Math.Round(qPriceHistories.Last().normPrice, 0);
                    }
                    else
                    {
                        avgImp = timelinePoints.Count > 0 ? timelinePoints.Last().AvgImportPrice : latestImportPrice;
                    }

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

                    var mPriceHistories = priceHistories
                        .Where(h => h.EffectiveDate >= cur && h.EffectiveDate <= mEnd)
                        .Select(h => Normalize(h.PurchaseUoMId, h.NewPrice, 1))
                        .ToList();

                    var mSales = orderDetails
                        .Where(o => o.Order.OrderDate >= cur && o.Order.OrderDate <= mEnd)
                        .Select(o => Normalize(o.UoMId, o.UnitPrice, o.Quantity))
                        .ToList();

                    decimal mImpQty = mImports.Sum(x => x.normQty);
                    decimal avgImp;
                    if (mImpQty > 0)
                    {
                        avgImp = Math.Round(mImports.Sum(x => x.normPrice * x.normQty) / mImpQty, 0);
                    }
                    else if (mPriceHistories.Count > 0)
                    {
                        avgImp = Math.Round(mPriceHistories.Last().normPrice, 0);
                    }
                    else
                    {
                        avgImp = timelinePoints.Count > 0 ? timelinePoints.Last().AvgImportPrice : latestImportPrice;
                    }

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

            // 8. Danh sách giao dịch chi tiết (Trích xuất cân đối giữa Đơn nhập PO, Đơn bán và Lịch sử giá)
            var transactions = new List<PriceTransactionDetailDto>();

            foreach (var po in poDetails.OrderByDescending(p => p.PurchaseOrder.OrderDate).Take(7))
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

            foreach (var ord in orderDetails.OrderByDescending(o => o.Order.OrderDate).Take(7))
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

            foreach (var h in priceHistories.OrderByDescending(h => h.EffectiveDate).Take(6))
            {
                var norm = Normalize(h.PurchaseUoMId, h.NewPrice, 1);
                transactions.Add(new PriceTransactionDetailDto
                {
                    Date = h.EffectiveDate,
                    Type = "Điều chỉnh giá NCC",
                    DocumentCode = $"PRC-NCC-{h.Id:D4}",
                    PartnerName = h.Supplier?.Name ?? "Nhà cung cấp",
                    OriginalUnitPrice = h.NewPrice,
                    OriginalUoMName = h.PurchaseUoM?.Name ?? baseUoMName,
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
                CoefficientOfVariation = cvPercent,
                VolatilityLevel = volatilityLevel,
                MinImportPrice = minImportPrice,
                MaxImportPrice = maxImportPrice,
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

