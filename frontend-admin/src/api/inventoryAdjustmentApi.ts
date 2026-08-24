import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  InventoryAdjustment,
  InventoryAdjustmentCreatePayload,
  InventoryAdjustmentQueryParams,
} from '../types/inventoryAdjustment';

export const inventoryAdjustmentApi = {
  getAll: (params?: InventoryAdjustmentQueryParams): Promise<PagedResult<InventoryAdjustment>> => {
    return axiosClient.get('/InventoryAdjustments', { params });
  },

  getById: (id: number): Promise<InventoryAdjustment> => {
    return axiosClient.get(`/InventoryAdjustments/${id}`);
  },

  create: (data: InventoryAdjustmentCreatePayload): Promise<{ id: number; message?: string }> => {
    return axiosClient.post('/InventoryAdjustments', data);
  },

  approve: (id: number): Promise<{ message?: string }> => {
    return axiosClient.post(`/InventoryAdjustments/${id}/approve`);
  },

  cancel: (id: number, reason: string): Promise<{ message?: string }> => {
    return axiosClient.post(`/InventoryAdjustments/${id}/cancel`, { reason });
  },

  delete: (id: number): Promise<{ message?: string }> => {
    return axiosClient.delete(`/InventoryAdjustments/${id}`);
  },
};
