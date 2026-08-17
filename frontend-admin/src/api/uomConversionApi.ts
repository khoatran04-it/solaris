import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { UoMConversion, UoMConversionPayload, UoMConversionQueryParams } from '../types/uomConversion';

export const uomConversionApi = {
    // 1. GET Paged (Đã gọn gàng nhờ dùng QueryParams)
    getAll: (params?: UoMConversionQueryParams): Promise<PagedResult<UoMConversion>> => {
        return axiosClient.get('/UoMConversions', { params });
    },

    // 2. GET ALL (Không phân trang)
    getAllList: (isActiveOnly: boolean = false): Promise<UoMConversion[]> => {
        return axiosClient.get('/UoMConversions/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    // 3. GET by ID
    getById: (id: number): Promise<UoMConversion> => {
        return axiosClient.get(`/UoMConversions/${id}`);
    },

    // 4. POST
    create: (data: UoMConversionPayload): Promise<UoMConversion> => {
        return axiosClient.post('/UoMConversions', data);
    },

    // 5. PUT
    update: (id: number, data: UoMConversionPayload): Promise<void> => {
        return axiosClient.put(`/UoMConversions/${id}`, data);
    },

    // 6. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/UoMConversions/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/UoMConversions/${id}/toggle-active`);
    }
};