import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
    InventoryIssue,
    InventoryIssueCreatePayload,
    InventoryIssueQueryParams,
    SuggestedBatch
} from '../types/inventoryIssue';

export const inventoryIssueApi = {
    getAll: (params?: InventoryIssueQueryParams): Promise<PagedResult<InventoryIssue>> => {
        return axiosClient.get('/InventoryIssues', { params });
    },

    getById: (id: number): Promise<InventoryIssue> => {
        return axiosClient.get(`/InventoryIssues/${id}`);
    },

    getSuggestedBatches: (warehouseId: number, variantId: number, neededQuantity: number): Promise<SuggestedBatch[]> => {
        return axiosClient.get('/InventoryIssues/suggested-batches', {
            params: { warehouseId, variantId, neededQuantity }
        });
    },

    create: (data: InventoryIssueCreatePayload): Promise<{ id: number }> => {
        return axiosClient.post('/InventoryIssues', data);
    },

    complete: (id: number, note?: string): Promise<void> => {
        return axiosClient.post(`/InventoryIssues/${id}/complete`, { note });
    },

    cancel: (id: number, reason: string): Promise<void> => {
        return axiosClient.post(`/InventoryIssues/${id}/cancel`, { reason });
    },
};
