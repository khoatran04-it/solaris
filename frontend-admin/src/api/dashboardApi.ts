import axiosClient from './axiosClient';
import type {
  DashboardOverviewDto,
  DashboardSalesGeographyDto,
  DashboardInventoryCapacityDto,
  DashboardQualityExpiryDto,
  DashboardFinancialPerformanceDto,
  DashboardPriceVolatilityDto,
  SkuSelectItem,
  PriceVolatilityTimeframe,
  DashboardPeriod,
} from '../types/dashboard';

export const dashboardApi = {
  getOverview: (
    period: DashboardPeriod = '30days',
    fromDate?: string,
    toDate?: string
  ): Promise<DashboardOverviewDto> => {
    const params: Record<string, string> = { period };
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    return axiosClient.get('/dashboard/overview', { params });
  },

  getSalesGeography: (
    period: DashboardPeriod = '30days',
    fromDate?: string,
    toDate?: string
  ): Promise<DashboardSalesGeographyDto> => {
    const params: Record<string, string> = { period };
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    return axiosClient.get('/dashboard/sales-geography', { params });
  },

  getInventoryCapacity: (): Promise<DashboardInventoryCapacityDto> => {
    return axiosClient.get('/dashboard/inventory-capacity');
  },

  getQualityExpiry: (): Promise<DashboardQualityExpiryDto> => {
    return axiosClient.get('/dashboard/quality-expiry');
  },

  getFinancialPerformance: (
    period: DashboardPeriod = '30days',
    fromDate?: string,
    toDate?: string
  ): Promise<DashboardFinancialPerformanceDto> => {
    const params: Record<string, string> = { period };
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    return axiosClient.get('/dashboard/financial-performance', { params });
  },

  getPriceVolatility: (
    variantId?: number,
    timeframe: PriceVolatilityTimeframe = 'month',
    fromDate?: string,
    toDate?: string
  ): Promise<DashboardPriceVolatilityDto> => {
    const params: Record<string, any> = { timeframe };
    if (variantId) params.variantId = variantId;
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;
    return axiosClient.get('/dashboard/price-volatility', { params });
  },

  getPriceVolatilitySkus: (): Promise<SkuSelectItem[]> => {
    return axiosClient.get('/dashboard/price-volatility/skus');
  },
};
