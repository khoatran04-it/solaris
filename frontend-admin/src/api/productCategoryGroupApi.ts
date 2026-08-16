import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    ProductCategoryGroup, 
    ProductCategoryGroupPayload, 
    ProductCategoryGroupQueryParams 
} from '../types/productCategoryGroup';

export const productCategoryGroupApi = {
    // 1. GET (Phân trang & Lọc)
    getAll: (params?: ProductCategoryGroupQueryParams): Promise<PagedResult<ProductCategoryGroup>> => {
        return axiosClient.get('/ProductCategoryGroups', { params });
    },

    // 2. GET ALL (Không phân trang, dùng cho dropdown)
    getAllList: (): Promise<ProductCategoryGroup[]> => {
        return axiosClient.get('/ProductCategoryGroups/all');
    },

    // 3. GET by ID
    getById: (id: number): Promise<ProductCategoryGroup> => {
        return axiosClient.get(`/ProductCategoryGroups/${id}`);
    },

    // 4. POST (Trả về message và id theo đúng chuẩn Backend C#)
    create: (data: ProductCategoryGroupPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/ProductCategoryGroups', data);
    },

    // 5. PUT
    update: (id: number, data: ProductCategoryGroupPayload): Promise<void> => {
        return axiosClient.put(`/ProductCategoryGroups/${id}`, data);
    },

    // 6. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/ProductCategoryGroups/${id}`);
    },
};