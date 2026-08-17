import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { CustomerGroup, CustomerGroupPayload, CustomerGroupQueryParams } from '../types/customerGroup';

export const customerGroupApi = {
    // 1. GET (Phân trang và bộ lọc)
    getAll: (params?: CustomerGroupQueryParams): Promise<PagedResult<CustomerGroup>> => {
        return axiosClient.get('/CustomerGroups', { params });
    },

    // 2. GET ALL (Dropdown/Lookup)
    getAllList: (isActiveOnly: boolean = false): Promise<CustomerGroup[]> => {
        return axiosClient.get('/CustomerGroups/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    // 3. GET by ID
    getById: (id: number): Promise<CustomerGroup> => {
        return axiosClient.get(`/CustomerGroups/${id}`);
    },

    // 4. POST
    create: (data: CustomerGroupPayload): Promise<CustomerGroup> => {
        return axiosClient.post('/CustomerGroups', data);
    },

    // 5. PUT
    update: (id: number, data: CustomerGroupPayload): Promise<void> => {
        return axiosClient.put(`/CustomerGroups/${id}`, data);
    },

    // 6. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/CustomerGroups/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Hoạt động / Khóa)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/CustomerGroups/${id}/toggle-active`);
    }
};