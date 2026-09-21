import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  InventoryTransfer,
  InventoryTransferCreatePayload,
  InventoryTransferQueryParams,
} from '../types/inventoryTransfer';

export const inventoryTransferApi = {
  getAll: (params?: InventoryTransferQueryParams): Promise<PagedResult<InventoryTransfer>> => {
    return axiosClient.get('/InventoryTransfers', { params });
  },

  getById: (id: number): Promise<InventoryTransfer> => {
    return axiosClient.get(`/InventoryTransfers/${id}`);
  },

  create: (data: InventoryTransferCreatePayload): Promise<{ id: number }> => {
    return axiosClient.post('/InventoryTransfers', data);
  },

  approve: (id: number, note?: string): Promise<void> => {
    return axiosClient.post(`/InventoryTransfers/${id}/approve`, { note });
  },

  dispatch: (id: number): Promise<void> => {
    return axiosClient.post(`/InventoryTransfers/${id}/dispatch`);
  },

  receive: (id: number): Promise<void> => {
    return axiosClient.post(`/InventoryTransfers/${id}/receive`);
  },

  inspectAndReceive: (id: number, data: any): Promise<void> => {
    return axiosClient.post(`/InventoryTransfers/${id}/inspect-and-receive`, data);
  },

  cancel: (id: number, reason: string): Promise<void> => {
    return axiosClient.post(`/InventoryTransfers/${id}/cancel`, { reason });
  },
};
