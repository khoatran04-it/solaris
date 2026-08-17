import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    ProductCategory, 
    ProductCategoryPayload, 
    ProductCategoryQueryParams 
} from '../types/productCategory';

export const productCategoryApi = {
    // 1. GET (Phân trang & Lọc)
    getAll: (params?: ProductCategoryQueryParams): Promise<PagedResult<ProductCategory>> => {
        return axiosClient.get('/ProductCategories', { params });
    },

    // 2. GET ALL (Không phân trang, dùng cho dropdown)
    getAllList: (isActiveOnly: boolean = false): Promise<ProductCategory[]> => {
        return axiosClient.get('/ProductCategories/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    // 3. GET by ID
    getById: (id: number): Promise<ProductCategory> => {
        return axiosClient.get(`/ProductCategories/${id}`);
    },

    // 4. POST
    create: (data: ProductCategoryPayload): Promise<ProductCategory> => {
        return axiosClient.post('/ProductCategories', data);
    },

    // 5. PUT
    update: (id: number, data: ProductCategoryPayload): Promise<void> => {
        return axiosClient.put(`/ProductCategories/${id}`, data);
    },

    // 6. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/ProductCategories/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/ProductCategories/${id}/toggle-active`);
    }
};