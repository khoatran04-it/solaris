import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { CustomerTier, CustomerTierPayload, CustomerTierQueryParams } from '../types/customerTier';

export const customerTierApi = {
  // 1. GET (Phân trang và bộ lọc)
  getAll: (params?: CustomerTierQueryParams): Promise<PagedResult<CustomerTier>> => {
    return axiosClient.get('/CustomerTiers', { params });
  },

  // 2. GET ALL (Không phân trang, dùng cho dropdown)
  getAllList: (): Promise<CustomerTier[]> => {
    return axiosClient.get('/CustomerTiers/all');
  },

  // 3. GET by ID
  getById: (id: number): Promise<CustomerTier> => {
    return axiosClient.get(`/CustomerTiers/${id}`);
  },

  // 4. POST
  create: (data: CustomerTierPayload): Promise<CustomerTier> => {
    return axiosClient.post('/CustomerTiers', data);
  },

  // 5. PUT
  update: (id: number, data: CustomerTierPayload): Promise<void> => {
    return axiosClient.put(`/CustomerTiers/${id}`, data);
  },

  // 6. DELETE
  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/CustomerTiers/${id}`);
  },

  // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/CustomerTiers/${id}/toggle-active`);
  },
};
