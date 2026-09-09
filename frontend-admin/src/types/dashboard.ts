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

// --- Dashboard 5: Financial & Cash Flow Performance ---
export interface FinancialTimelinePoint {
  label: string;
  revenue: number;
  cogs: number;
  grossProfit: number;
  cashInflow: number;
  cashOutflow: number;
}

export interface CashFlowBridgeDto {
  grossSales: number;
  onlinePaymentInflow: number;
  codCollectedInflow: number;
  codInTransitAmount: number;
  customerRefundOutflow: number;
  inboundGoodsReceiptOutflow: number;
  pendingPoCommitment: number;
  netOperatingCashFlow: number;
}

export interface SupplierPayableItem {
  supplierId: number;
  supplierName: string;
  supplierCode: string;
  totalPoCount: number;
  totalPoValue: number;
  receivedValue: number;
  qcRejectedValue: number;
  pendingCommitment: number;
}

export interface CategoryProfitabilityItem {
  categoryGroupName: string;
  revenue: number;
  cogs: number;
  grossProfit: number;
  grossMarginPercent: number;
  quantitySold: number;
}

export interface ShrinkageLossDto {
  damagedStockValue: number;
  expiringStockRiskValue: number;
  returnRefundLoss: number;
  totalShrinkageLoss: number;
}

export interface DashboardFinancialPerformanceDto {
  grossRevenue: number;
  customerRefunds: number;
  netRevenue: number;
  totalCogs: number;
  grossProfit: number;
  grossMarginPercent: number;
  totalPoValue: number;
  totalGoodsReceivedValue: number;
  estimatedNetCashFlow: number;
  netRevenueGrowthPercent: number;
  cashFlowBridge: CashFlowBridgeDto;
  timeline: FinancialTimelinePoint[];
  supplierPayables: SupplierPayableItem[];
  categoryProfitability: CategoryProfitabilityItem[];
  shrinkageLoss: ShrinkageLossDto;
}

// --- Dashboard 6: Price Volatility & Margin Spread ---
export type PriceVolatilityTimeframe = 'week' | 'month' | 'year';

export interface SkuSelectItem {
  variantId: number;
  variantName: string;
  variantCode: string;
  productName: string;
  baseUoMName: string;
}

export interface PriceVolatilityPoint {
  label: string;
  avgImportPrice: number;
  avgSellingPrice: number;
  spread: number;
  marginPercent: number;
  isProfit: boolean;
}

export interface PriceTransactionDetail {
  date: string;
  type: string;
  documentCode: string;
  partnerName: string;
  originalUnitPrice: number;
  originalUoMName: string;
  normalizedUnitPrice: number;
  quantityInBaseUoM: number;
  baseUoMName: string;
}

export interface DashboardPriceVolatilityDto {
  variantId: number;
  variantName: string;
  variantCode: string;
  productName: string;
  baseUoMName: string;
  latestImportPrice: number;
  currentSellingPrice: number;
  priceSpread: number;
  marginPercent: number;
  importPriceChangePercent: number;
  isLossMaking: boolean;
  timeline: PriceVolatilityPoint[];
  transactions: PriceTransactionDetail[];
}
