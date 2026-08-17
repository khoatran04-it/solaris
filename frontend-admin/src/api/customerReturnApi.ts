import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
    CustomerReturn,
    CustomerReturnCreatePayload,
    CustomerReturnInspectionPayload,
    CustomerReturnQueryParams
} from '../types/customerReturn';

export const customerReturnApi = {
    getAll: (params?: CustomerReturnQueryParams): Promise<PagedResult<CustomerReturn>> => {
        return axiosClient.get('/CustomerReturns', { params });
    },

    getById: (id: number): Promise<CustomerReturn> => {
        return axiosClient.get(`/CustomerReturns/${id}`);
    },

    create: (data: CustomerReturnCreatePayload): Promise<{ id: number }> => {
        return axiosClient.post('/CustomerReturns', data);
    },

    inspectAndComplete: (id: number, data: CustomerReturnInspectionPayload): Promise<void> => {
        return axiosClient.post(`/CustomerReturns/${id}/inspect-and-complete`, data);
    },

    reject: (id: number, reason: string): Promise<void> => {
        return axiosClient.post(`/CustomerReturns/${id}/reject`, { reason });
    },
};
