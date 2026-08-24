import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  Order,
  OrderCreatePayload,
  OrderUpdatePayload,
  OrderQueryParams,
  RoutingPreviewResult,
} from '../types/order';

export const orderApi = {
  getAll: (params?: OrderQueryParams): Promise<PagedResult<Order>> => {
    return axiosClient.get('/Orders', { params });
  },

  getById: (id: number): Promise<Order> => {
    return axiosClient.get(`/Orders/${id}`);
  },

  create: (data: OrderCreatePayload): Promise<{ id: number; message?: string }> => {
    return axiosClient.post('/Orders', data);
  },

  previewRouting: (data: OrderCreatePayload): Promise<RoutingPreviewResult> => {
    return axiosClient.post('/Orders/routing-preview', data);
  },

  updateStatus: (id: number, data: OrderUpdatePayload): Promise<{ message?: string }> => {
    return axiosClient.put(`/Orders/${id}/status`, data);
  },

  cancel: (id: number, reason: string): Promise<{ message?: string }> => {
    return axiosClient.post(`/Orders/${id}/cancel`, { reason });
  },

  createGhnOrder: (
    id: number
  ): Promise<{ orderCode: string; expectedDeliveryDate?: string; totalFee?: number }> => {
    return axiosClient.post(`/shipping/ghn/create-order/${id}`);
  },

  delete: (id: number): Promise<{ message?: string }> => {
    return axiosClient.delete(`/Orders/${id}`);
  },
};
