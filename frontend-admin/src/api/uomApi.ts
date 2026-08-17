import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { UoM, UoMPayload, UoMQueryParams } from '../types/uom';

export const uomApi = {
    // 1. GET Paged (Dùng cho bảng danh sách có filter theo categoryId)
    getAll: (params?: UoMQueryParams): Promise<PagedResult<UoM>> => {
        return axiosClient.get('/UoMs', { params });
    },

    // 2. GET ALL (Không phân trang - Dùng để làm Dropdown cho chức năng khác)
    getAllList: (isActiveOnly: boolean = false): Promise<UoM[]> => {
        return axiosClient.get('/UoMs/all', { params: { isActive: isActiveOnly ? true : undefined } });
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

    // 6. DELETE (Xóa - Backend đã có logic chặn xóa nếu đang làm BaseUoM, dùng trong SP, etc.)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/UoMs/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/UoMs/${id}/toggle-active`);
    }
};