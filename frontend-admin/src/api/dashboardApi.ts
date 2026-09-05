import axiosClient from './axiosClient';
import type {
  DashboardOverviewDto,
  DashboardSalesGeographyDto,
  DashboardInventoryCapacityDto,
  DashboardQualityExpiryDto,
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
};
