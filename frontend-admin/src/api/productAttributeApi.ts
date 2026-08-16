import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    ProductAttribute, 
    ProductAttributePayload, 
    ProductAttributeQueryParams 
} from '../types/productAttribute';

export const productAttributeApi = {
    getAll: (params?: ProductAttributeQueryParams): Promise<PagedResult<ProductAttribute>> => {
        return axiosClient.get('/ProductAttributes', { params });
    },

    getAllList: (): Promise<ProductAttribute[]> => {
        return axiosClient.get('/ProductAttributes/all');
    },

    getById: (id: number): Promise<ProductAttribute> => {
        return axiosClient.get(`/ProductAttributes/${id}`);
    },

    create: (data: ProductAttributePayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/ProductAttributes', data);
    },

    update: (id: number, data: ProductAttributePayload): Promise<void> => {
        return axiosClient.put(`/ProductAttributes/${id}`, data);
    },

    // Cập nhật hàng loạt (Clear & Replace) cho 1 biến thể
    updateBulk: (variantId: number, data: ProductAttributePayload[]): Promise<{ message: string }> => {
        return axiosClient.put(`/ProductAttributes/bulk/${variantId}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/ProductAttributes/${id}`);
    },
};