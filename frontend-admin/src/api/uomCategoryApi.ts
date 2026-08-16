import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { UoMCategory, UoMCategoryPayload } from '../types/uomCategory';

export const uomCategoryApi = {
    // 1. GET Paged (Dành cho bảng danh sách có phân trang và filter)
    getAll: (params?: any): Promise<PagedResult<UoMCategory>> => {
        return axiosClient.get('/UoMCategories', { params });
    },

    // 2. GET ALL (Không phân trang - Dùng cho Dropdown Select ở form UoM)
    getAllList: (): Promise<UoMCategory[]> => {
        return axiosClient.get('/UoMCategories/all');
    },

    // 3. GET by ID (Dùng khi mở form Edit để fetch dữ liệu cũ)
    getById: (id: number): Promise<UoMCategory> => {
        return axiosClient.get(`/UoMCategories/${id}`);
    },

    // 4. POST (Thêm mới)
    create: (data: UoMCategoryPayload): Promise<UoMCategory> => {
        return axiosClient.post('/UoMCategories', data);
    },

    // 5. PUT (Cập nhật toàn bộ)
    update: (id: number, data: UoMCategoryPayload): Promise<void> => {
        return axiosClient.put(`/UoMCategories/${id}`, data);
    },

    // 6. DELETE (Xóa)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/UoMCategories/${id}`);
    },
};