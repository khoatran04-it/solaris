import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  PurchaseOrder,
  PurchaseOrderCreatePayload,
  PurchaseOrderUpdatePayload,
  PurchaseOrderQueryParams,
} from '../types/purchaseOrder';

export const purchaseOrderApi = {
  getAll: (params?: PurchaseOrderQueryParams): Promise<PagedResult<PurchaseOrder>> => {
    return axiosClient.get('/PurchaseOrders', { params });
  },

  getById: (id: number): Promise<PurchaseOrder> => {
    return axiosClient.get(`/PurchaseOrders/${id}`);
  },

  create: (data: PurchaseOrderCreatePayload): Promise<{ id: number }> => {
    return axiosClient.post('/PurchaseOrders', data);
  },

  update: (id: number, data: PurchaseOrderCreatePayload): Promise<void> => {
    return axiosClient.put(`/PurchaseOrders/${id}`, data);
  },

  updateStatus: (id: number, data: PurchaseOrderUpdatePayload): Promise<void> => {
    return axiosClient.put(`/PurchaseOrders/${id}/status`, data);
  },

  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/PurchaseOrders/${id}`);
  },
};
