import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
    Order,
    OrderCreatePayload,
    OrderUpdatePayload,
    OrderQueryParams,
    RoutingPreviewResult
} from '../types/order';

export const orderApi = {
    getAll: (params?: OrderQueryParams): Promise<PagedResult<Order>> => {
        return axiosClient.get('/Orders', { params });
    },

    getById: (id: number): Promise<Order> => {
        return axiosClient.get(`/Orders/${id}`);
    },

    create: (data: OrderCreatePayload): Promise<{ id: number }> => {
        return axiosClient.post('/Orders', data);
    },

    previewRouting: (data: OrderCreatePayload): Promise<RoutingPreviewResult> => {
        return axiosClient.post('/Orders/routing-preview', data);
    },

    updateStatus: (id: number, data: OrderUpdatePayload): Promise<void> => {
        return axiosClient.put(`/Orders/${id}/status`, data);
    },

    cancel: (id: number, reason: string): Promise<void> => {
        return axiosClient.post(`/Orders/${id}/cancel`, { reason });
    },
};
