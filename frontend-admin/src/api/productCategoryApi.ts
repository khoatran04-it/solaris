import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    ProductCategory, 
    ProductCategoryPayload, 
    ProductCategoryQueryParams 
} from '../types/productCategory';

export const productCategoryApi = {
    getAll: (params?: ProductCategoryQueryParams): Promise<PagedResult<ProductCategory>> => {
        return axiosClient.get('/ProductCategories', { params });
    },

    getAllList: (): Promise<ProductCategory[]> => {
        return axiosClient.get('/ProductCategories/all');
    },

    getById: (id: number): Promise<ProductCategory> => {
        return axiosClient.get(`/ProductCategories/${id}`);
    },

    create: (data: ProductCategoryPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/ProductCategories', data);
    },

    update: (id: number, data: ProductCategoryPayload): Promise<void> => {
        return axiosClient.put(`/ProductCategories/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/ProductCategories/${id}`);
    },
};