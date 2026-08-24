import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  InventoryAudit,
  InventoryAuditCreatePayload,
  InventoryAuditSubmitCountPayload,
  InventoryAuditQueryParams,
} from '../types/inventoryAudit';

export const inventoryAuditApi = {
  getAll: (params?: InventoryAuditQueryParams): Promise<PagedResult<InventoryAudit>> => {
    return axiosClient.get('/InventoryAudits', { params });
  },

  getById: (id: number): Promise<InventoryAudit> => {
    return axiosClient.get(`/InventoryAudits/${id}`);
  },

  create: (data: InventoryAuditCreatePayload): Promise<{ id: number }> => {
    return axiosClient.post('/InventoryAudits', data);
  },

  submitCount: (id: number, data: InventoryAuditSubmitCountPayload): Promise<void> => {
    return axiosClient.post(`/InventoryAudits/${id}/submit-count`, data);
  },

  approveAndReconcile: (id: number): Promise<{ adjustmentId: number; message: string }> => {
    return axiosClient.post(`/InventoryAudits/${id}/approve-and-reconcile`);
  },

  cancel: (id: number, reason: string): Promise<void> => {
    return axiosClient.post(`/InventoryAudits/${id}/cancel`, { reason });
  },
};
