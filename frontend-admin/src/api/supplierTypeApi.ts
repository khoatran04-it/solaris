import axiosClient from './axiosClient';
import { PagedResult } from '../types/common'; // Xóa SupplierTypeQueryParams
import { SupplierType, SupplierTypePayload, SupplierTypeQueryParams } from '../types/supplierType';

export const supplierTypeApi = {
  // 1. GET
  getAll: (params?: SupplierTypeQueryParams): Promise<PagedResult<SupplierType>> => {
    return axiosClient.get('/SupplierTypes', { params });
  },

  getAllList: (): Promise<SupplierType[]> => {
    return axiosClient.get('/SupplierTypes/all');
  },

  // 2. GET by ID
  getById: (id: number): Promise<SupplierType> => {
    return axiosClient.get(`/SupplierTypes/${id}`);
  },

  // 3. POST
  create: (data: SupplierTypePayload): Promise<SupplierType> => {
    return axiosClient.post('/SupplierTypes', data);
  },

  // 4. PUT
  update: (id: number, data: SupplierTypePayload): Promise<void> => {
    return axiosClient.put(`/SupplierTypes/${id}`, data);
  },

  // 5. DELETE
  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/SupplierTypes/${id}`);
  },

  // 6. PATCH (Thay đổi trạng thái Tạm khóa / Hoạt động)
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/SupplierTypes/${id}/toggle-active`);
  },
};
