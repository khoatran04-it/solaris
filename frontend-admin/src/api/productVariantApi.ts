import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    ProductVariant, 
    ProductVariantPayload, 
    ProductVariantQueryParams 
} from '../types/productVariant';

export const productVariantApi = {
    // 1. GET (Có phân trang & Lọc)
    getAll: (params?: ProductVariantQueryParams): Promise<PagedResult<ProductVariant>> => {
        return axiosClient.get('/ProductVariants', { params });
    },

    // 2. GET ALL (Lấy tất cả)
    getAllList: (): Promise<ProductVariant[]> => {
        return axiosClient.get('/ProductVariants/all');
    },

    // 3. GET by ID (Lấy chi tiết bao gồm cả Thuộc tính & tính giá KM)
    getById: (id: number): Promise<ProductVariant> => {
        return axiosClient.get(`/ProductVariants/${id}`);
    },

    // 4. POST (Thêm mới biến thể & Gắn thuộc tính bằng Transaction)
    create: (data: ProductVariantPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/ProductVariants', data);
    },

    // 5. PUT (Cập nhật biến thể & Ghi đè thuộc tính)
    update: (id: number, data: ProductVariantPayload): Promise<void> => {
        return axiosClient.put(`/ProductVariants/${id}`, data);
    },

    // 6. DELETE (Xóa mềm)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/ProductVariants/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Tạm khóa / Hoạt động)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/ProductVariants/${id}/toggle-active`);
    }
};