import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { User, UserPayload, UserQueryParams, UserChangePasswordPayload } from '../types/user';

export const userApi = {
  getAll: (params?: UserQueryParams): Promise<PagedResult<User>> => {
    return axiosClient.get('/ia-users', { params });
  },

  getAllList: (): Promise<User[]> => {
    return axiosClient.get('/ia-users/all');
  },

  getById: (id: number): Promise<User> => {
    return axiosClient.get(`/ia-users/${id}`);
  },

  create: (data: UserPayload): Promise<{ message: string; id: number }> => {
    return axiosClient.post('/ia-users', data);
  },

  update: (id: number, data: UserPayload): Promise<void> => {
    return axiosClient.put(`/ia-users/${id}`, data);
  },

  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/ia-users/${id}`);
  },

  // Bổ sung các hàm đặc thù của User
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/ia-users/${id}/toggle-active`);
  },

  changePassword: (id: number, data: UserChangePasswordPayload): Promise<void> => {
    return axiosClient.patch(`/ia-users/${id}/change-password`, data);
  },
};
