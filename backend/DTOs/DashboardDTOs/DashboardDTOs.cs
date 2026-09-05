namespace backend.DTOs.DashboardDTOs
{
    // ============================================================
    // DASHBOARD 1: OVERVIEW — Tổng quan Kinh doanh & Doanh thu
    // ============================================================

    public class RevenueTimelinePoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class PaymentMethodBreakdownItem
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Percent { get; set; }
    }

    public class OrderStatusPipelineItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class RecentOrderItem
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }

    public class DashboardOverviewDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowthPercent { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal FulfillmentRatePercent { get; set; }
        public List<RevenueTimelinePoint> RevenueTimeline { get; set; } = new();
        public List<PaymentMethodBreakdownItem> PaymentMethodBreakdown { get; set; } = new();
        public List<OrderStatusPipelineItem> OrderStatusPipeline { get; set; } = new();
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    // ============================================================
    // DASHBOARD 2: SALES & GEOGRAPHY
    // ============================================================

    public class TopProductItem
    {
        public int VariantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string UoM { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenuePercent { get; set; }
    }

    public class CategoryBreakdownItem
    {
        public string CategoryGroupName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int QuantitySold { get; set; }
        public decimal Percent { get; set; }
    }

    public class GeographyBreakdownItem
    {
        public string ProvinceName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percent { get; set; }
    }

    public class CustomerTierBreakdownItem
    {
        public string TierName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percent { get; set; }
    }

    public class DashboardSalesGeographyDto
    {
        public List<TopProductItem> TopProducts { get; set; } = new();
        public List<CategoryBreakdownItem> CategoryBreakdown { get; set; } = new();
        public List<GeographyBreakdownItem> GeographyBreakdown { get; set; } = new();
        public List<CustomerTierBreakdownItem> CustomerTierBreakdown { get; set; } = new();
    }

    // ============================================================
    // DASHBOARD 3: INVENTORY & CAPACITY
    // ============================================================

    public class WarehouseCapacityItem
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public decimal TotalCbm { get; set; }
        public decimal OccupiedCbm { get; set; }
        public decimal OccupancyCbmPercent { get; set; }
        public decimal MaxWeightKg { get; set; }
        public decimal OccupiedWeightKg { get; set; }
        public decimal OccupancyWeightPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAreaSqm { get; set; }
        public int WarningThresholdPercent { get; set; }
    }

    public class InventoryCompartmentsDto
    {
        public int AvailableQty { get; set; }
        public int ReservedQty { get; set; }
        public int InQcQty { get; set; }
        public int DamagedQty { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class TopSpaceConsumingItem
    {
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;
        public decimal TotalCbm { get; set; }
        public int TotalQty { get; set; }
    }

    public class LowStockAlertItem
    {
        public string VariantName { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int AvailableQty { get; set; }
    }

    public class DashboardInventoryCapacityDto
    {
        public decimal TotalStockValue { get; set; }
        public int TotalActiveWarehouses { get; set; }
        public List<WarehouseCapacityItem> WarehouseCapacities { get; set; } = new();
        public InventoryCompartmentsDto InventoryCompartments { get; set; } = new();
        public List<TopSpaceConsumingItem> TopSpaceConsumingProducts { get; set; } = new();
        public List<LowStockAlertItem> LowStockAlerts { get; set; } = new();
    }

    // ============================================================
    // DASHBOARD 4: QUALITY & EXPIRY
    // ============================================================

    public class ExpiryOverviewDto
    {
        public int ExpiredCount { get; set; }
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int SafeCount { get; set; }
    }

    public class ExpiringBatchItem
    {
        public string BatchCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public int QuantityAvailable { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal EstimatedLossValue { get; set; }
    }

    public class QcRejectReasonItem
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percent { get; set; }
    }

    public class DashboardQualityExpiryDto
    {
        public ExpiryOverviewDto ExpiryOverview { get; set; } = new();
        public decimal InboundQcRejectRatePercent { get; set; }
        public int TotalInboundItems { get; set; }
        public int TotalRejectedItems { get; set; }
        public decimal CustomerReturnRatePercent { get; set; }
        public int TotalOrders { get; set; }
        public int TotalReturnOrders { get; set; }
        public List<ExpiringBatchItem> ExpiringBatches { get; set; } = new();
        public List<QcRejectReasonItem> QcRejectReasons { get; set; } = new();
    }
}
