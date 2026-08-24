import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { CustomerType, CustomerTypePayload, CustomerTypeQueryParams } from '../types/customerType';

export const customerTypeApi = {
  // 1. GET (Phân trang và bộ lọc)
  getAll: (params?: CustomerTypeQueryParams): Promise<PagedResult<CustomerType>> => {
    return axiosClient.get('/CustomerTypes', { params });
  },

  // 2. GET ALL (Không phân trang, dùng cho dropdown)
  getAllList: (): Promise<CustomerType[]> => {
    return axiosClient.get('/CustomerTypes/all');
  },

  // 3. GET by ID
  getById: (id: number): Promise<CustomerType> => {
    return axiosClient.get(`/CustomerTypes/${id}`);
  },

  // 4. POST
  create: (data: CustomerTypePayload): Promise<CustomerType> => {
    return axiosClient.post('/CustomerTypes', data);
  },

  // 5. PUT
  update: (id: number, data: CustomerTypePayload): Promise<void> => {
    return axiosClient.put(`/CustomerTypes/${id}`, data);
  },

  // 6. DELETE
  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/CustomerTypes/${id}`);
  },

  // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/CustomerTypes/${id}/toggle-active`);
  },
};
