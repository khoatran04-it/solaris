import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { UoM, UoMPayload } from '../types/uom';

export const uomApi = {
    // 1. GET Paged (Dùng cho bảng danh sách có filter theo categoryId)
    getAll: (params?: any): Promise<PagedResult<UoM>> => {
        return axiosClient.get('/UoMs', { params });
    },

    // 2. GET ALL (Không phân trang - Dùng để làm Dropdown cho chức năng khác)
    getAllList: (): Promise<UoM[]> => {
        return axiosClient.get('/UoMs/all');
    },

    // 3. GET by ID (Lấy chi tiết đổ vào form Edit)
    getById: (id: number): Promise<UoM> => {
        return axiosClient.get(`/UoMs/${id}`);
    },

    // 4. POST (Thêm mới đơn vị tính)
    create: (data: UoMPayload): Promise<UoM> => {
        return axiosClient.post('/UoMs', data);
    },

    // 5. PUT (Cập nhật đơn vị tính)
    update: (id: number, data: UoMPayload): Promise<void> => {
        return axiosClient.put(`/UoMs/${id}`, data);
    },

    // 6. DELETE (Xóa - Backend đã có logic chặn xóa nếu đang làm BaseUoM)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/UoMs/${id}`);
    },
};