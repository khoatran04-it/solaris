// ============================================================
// DASHBOARD TYPES — Solaris Admin
// ============================================================

// --- Dashboard 1: Overview ---
export interface RevenueTimelinePoint {
  label: string;
  revenue: number;
  orderCount: number;
}

export interface PaymentMethodBreakdownItem {
  method: string;
  count: number;
  amount: number;
  percent: number;
}

export interface OrderStatusPipelineItem {
  status: string;
  count: number;
}

export interface RecentOrderItem {
  id: number;
  orderCode: string;
  customerName: string;
  totalAmount: number;
  status: string;
  orderDate: string;
}

export interface DashboardOverviewDto {
  totalRevenue: number;
  revenueGrowthPercent: number;
  totalOrders: number;
  completedOrders: number;
  averageOrderValue: number;
  fulfillmentRatePercent: number;
  revenueTimeline: RevenueTimelinePoint[];
  paymentMethodBreakdown: PaymentMethodBreakdownItem[];
  orderStatusPipeline: OrderStatusPipelineItem[];
  recentOrders: RecentOrderItem[];
}

// --- Dashboard 2: Sales & Geography ---
export interface TopProductItem {
  variantId: number;
  name: string;
  code: string;
  uoM: string;
  quantitySold: number;
  totalRevenue: number;
  revenuePercent: number;
}

export interface CategoryBreakdownItem {
  categoryGroupName: string;
  totalRevenue: number;
  quantitySold: number;
  percent: number;
}

export interface GeographyBreakdownItem {
  provinceName: string;
  orderCount: number;
  totalRevenue: number;
  percent: number;
}

export interface CustomerTierBreakdownItem {
  tierName: string;
  customerCount: number;
  orderCount: number;
  totalRevenue: number;
  percent: number;
}

export interface DashboardSalesGeographyDto {
  topProducts: TopProductItem[];
  categoryBreakdown: CategoryBreakdownItem[];
  geographyBreakdown: GeographyBreakdownItem[];
  customerTierBreakdown: CustomerTierBreakdownItem[];
}

// --- Dashboard 3: Inventory & Capacity ---
export interface WarehouseCapacityItem {
  warehouseId: number;
  warehouseName: string;
  warehouseCode: string;
  totalCbm: number;
  occupiedCbm: number;
  occupancyCbmPercent: number;
  maxWeightKg: number;
  occupiedWeightKg: number;
  occupancyWeightPercent: number;
  status: 'Safe' | 'Warning' | 'Critical';
  totalAreaSqm: number;
  warningThresholdPercent: number;
}

export interface InventoryCompartmentsDto {
  availableQty: number;
  reservedQty: number;
  inQcQty: number;
  damagedQty: number;
  totalValue: number;
}

export interface TopSpaceConsumingItem {
  variantName: string;
  variantCode: string;
  totalCbm: number;
  totalQty: number;
}

export interface LowStockAlertItem {
  variantName: string;
  variantCode: string;
  warehouseName: string;
  availableQty: number;
}

export interface DashboardInventoryCapacityDto {
  totalStockValue: number;
  totalActiveWarehouses: number;
  warehouseCapacities: WarehouseCapacityItem[];
  inventoryCompartments: InventoryCompartmentsDto;
  topSpaceConsumingProducts: TopSpaceConsumingItem[];
  lowStockAlerts: LowStockAlertItem[];
}

// --- Dashboard 4: Quality & Expiry ---
export interface ExpiryOverviewDto {
  expiredCount: number;
  criticalCount: number;
  warningCount: number;
  safeCount: number;
}

export interface ExpiringBatchItem {
  batchCode: string;
  productName: string;
  variantName: string;
  warehouseName: string;
  expiryDate: string;
  daysRemaining: number;
  quantityAvailable: number;
  unitPrice: number;
  estimatedLossValue: number;
}

export interface QcRejectReasonItem {
  reason: string;
  count: number;
  percent: number;
}

export interface DashboardQualityExpiryDto {
  expiryOverview: ExpiryOverviewDto;
  inboundQcRejectRatePercent: number;
  totalInboundItems: number;
  totalRejectedItems: number;
  customerReturnRatePercent: number;
  totalOrders: number;
  totalReturnOrders: number;
  expiringBatches: ExpiringBatchItem[];
  qcRejectReasons: QcRejectReasonItem[];
}

export type DashboardPeriod = 'today' | '7days' | '30days' | 'year';
