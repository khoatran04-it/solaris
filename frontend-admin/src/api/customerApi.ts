import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { Customer, CustomerPayload, CustomerQueryParams } from '../types/customer';

export const customerApi = {
    // 1. GET (Phân trang và bộ lọc)
    getAll: (params?: CustomerQueryParams): Promise<PagedResult<Customer>> => {
        return axiosClient.get('/Customers', { params });
    },

    // 2. GET ALL (Không phân trang, dùng cho dropdown)
    getAllList: (): Promise<Customer[]> => {
        return axiosClient.get('/Customers/all');
    },

    // 3. GET by ID
    getById: (id: number): Promise<Customer> => {
        return axiosClient.get(`/Customers/${id}`);
    },

    // 4. POST
    create: (data: CustomerPayload): Promise<Customer> => {
        return axiosClient.post('/Customers', data);
    },

    // 5. PUT
    update: (id: number, data: CustomerPayload): Promise<void> => {
        return axiosClient.put(`/Customers/${id}`, data);
    },

    // 6. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/Customers/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/Customers/${id}/toggle-active`);
    }
};