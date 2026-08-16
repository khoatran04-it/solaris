import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { CustomerGroup, CustomerGroupPayload } from '../types/customerGroup';

export const customerGroupApi = {
    getAll: (params?: any): Promise<PagedResult<CustomerGroup>> => {
        return axiosClient.get('/CustomerGroups', { params });
    },

    // Thêm tùy chọn lọc theo trạng thái hoạt động nếu cần
    getAllList: (isActiveOnly: boolean = false): Promise<CustomerGroup[]> => {
        return axiosClient.get('/CustomerGroups/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    getById: (id: number): Promise<CustomerGroup> => {
        return axiosClient.get(`/CustomerGroups/${id}`);
    },

    create: (data: CustomerGroupPayload): Promise<CustomerGroup> => {
        return axiosClient.post('/CustomerGroups', data);
    },

    update: (id: number, data: CustomerGroupPayload): Promise<void> => {
        return axiosClient.put(`/CustomerGroups/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/CustomerGroups/${id}`);
    },
};