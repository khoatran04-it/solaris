import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    InventoryReceipt, 
    InventoryReceiptCreatePayload, 
    InventoryReceiptQueryParams 
} from '../types/inventoryReceipt';

export const inventoryReceiptApi = {
    getAll: (params?: InventoryReceiptQueryParams): Promise<PagedResult<InventoryReceipt>> => {
        return axiosClient.get('/InventoryReceipts', { params });
    },

    getById: (id: number): Promise<InventoryReceipt> => {
        return axiosClient.get(`/InventoryReceipts/${id}`);
    },

    create: (data: InventoryReceiptCreatePayload): Promise<{ id: number }> => {
        return axiosClient.post('/InventoryReceipts', data);
    },

    complete: (id: number, note?: string): Promise<void> => {
        return axiosClient.post(`/InventoryReceipts/${id}/complete`, { note });
    },

    cancel: (id: number, reason: string): Promise<void> => {
        return axiosClient.post(`/InventoryReceipts/${id}/cancel`, { reason });
    },
};
