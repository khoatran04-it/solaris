import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    Role, 
    RolePayload, 
    RoleQueryParams 
} from '../types/role';

export const roleApi = {
    getAll: (params?: RoleQueryParams): Promise<PagedResult<Role>> => {
        return axiosClient.get('/ia-roles', { params });
    },

    getAllList: (): Promise<Role[]> => {
        return axiosClient.get('/ia-roles/all');
    },

    getById: (id: number): Promise<Role> => {
        return axiosClient.get(`/ia-roles/${id}`);
    },

    create: (data: RolePayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/ia-roles', data);
    },

    update: (id: number, data: RolePayload): Promise<void> => {
        return axiosClient.put(`/ia-roles/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/ia-roles/${id}`);
    },
        // 7. PATCH (Thay đổi trạng thái Tạm khóa / Hoạt động)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/ia-roles/${id}/toggle-active`);
    }
};