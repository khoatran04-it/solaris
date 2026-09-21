import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  PurchaseOrder,
  PurchaseOrderCreatePayload,
  PurchaseOrderUpdatePayload,
  PurchaseOrderQueryParams,
} from '../types/purchaseOrder';

export const purchaseOrderApi = {
  getAllList: (): Promise<PurchaseOrder[]> => {
    return axiosClient.get('/purchase-orders/all');
  },

  getAll: (params?: PurchaseOrderQueryParams): Promise<PagedResult<PurchaseOrder>> => {
    return axiosClient.get('/purchase-orders', { params });
  },

  getById: (id: number): Promise<PurchaseOrder> => {
    return axiosClient.get(`/purchase-orders/${id}`);
  },

  create: (data: PurchaseOrderCreatePayload): Promise<PurchaseOrder> => {
    return axiosClient.post('/purchase-orders', data);
  },

  update: (id: number, data: PurchaseOrderCreatePayload): Promise<{ message: string }> => {
    return axiosClient.put(`/purchase-orders/${id}`, data);
  },

  updateStatus: (id: number, data: PurchaseOrderUpdatePayload): Promise<{ message: string }> => {
    return axiosClient.put(`/purchase-orders/${id}/status`, data);
  },

  delete: (id: number): Promise<{ message: string }> => {
    return axiosClient.delete(`/purchase-orders/${id}`);
  },

  closeAndSettle: (id: number, reason: string): Promise<{ message: string }> => {
    return axiosClient.post(`/purchase-orders/${id}/close-and-settle`, { reason });
  },
};
