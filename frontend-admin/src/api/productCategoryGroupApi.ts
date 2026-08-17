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
    getAllList: (isActiveOnly: boolean = false): Promise<ProductCategoryGroup[]> => {
        return axiosClient.get('/ProductCategoryGroups/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    // 3. GET by ID
    getById: (id: number): Promise<ProductCategoryGroup> => {
        return axiosClient.get(`/ProductCategoryGroups/${id}`);
    },

    // 4. POST
    create: (data: ProductCategoryGroupPayload): Promise<ProductCategoryGroup> => {
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

    // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/ProductCategoryGroups/${id}/toggle-active`);
    }
};