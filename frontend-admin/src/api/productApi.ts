import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    Product, 
    ProductPayload, 
    ProductQueryParams,
    ProductAttributeConfig 
} from '../types/product';

export const productApi = {
    getAll: (params?: ProductQueryParams): Promise<PagedResult<Product>> => {
        return axiosClient.get('/Products', { params });
    },

    getAllList: (): Promise<Product[]> => {
        return axiosClient.get('/Products/all');
    },

    getById: (id: number): Promise<Product> => {
        return axiosClient.get(`/Products/${id}`);
    },

    create: (data: ProductPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/Products', data);
    },

    update: (id: number, data: ProductPayload): Promise<void> => {
        return axiosClient.put(`/Products/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/Products/${id}`);
    },

    // 🔥 BỔ SUNG: Bật/Tắt trạng thái sản phẩm gốc
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/Products/${id}/toggle-active`);
    },

    // 🔥 BỔ SUNG: Lấy cấu hình thuộc tính động (Dùng cho trang tạo ProductVariant)
    getAttributesConfig: (id: number): Promise<ProductAttributeConfig[]> => {
        return axiosClient.get(`/Products/${id}/attributes-config`);
    }
};