import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { CustomerTier, CustomerTierPayload } from '../types/customerTier';

export const customerTierApi = {
    getAll: (params?: any): Promise<PagedResult<CustomerTier>> => {
        return axiosClient.get('/CustomerTiers', { params });
    },

    getAllList: (): Promise<CustomerTier[]> => {
        return axiosClient.get('/CustomerTiers/all');
    },

    getById: (id: number): Promise<CustomerTier> => {
        return axiosClient.get(`/CustomerTiers/${id}`);
    },

    create: (data: CustomerTierPayload): Promise<CustomerTier> => {
        return axiosClient.post('/CustomerTiers', data);
    },

    update: (id: number, data: CustomerTierPayload): Promise<void> => {
        return axiosClient.put(`/CustomerTiers/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/CustomerTiers/${id}`);
    }
};