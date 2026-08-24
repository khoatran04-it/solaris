import axiosClient from './axiosClient';
import { SupplierAddressPayload } from '../types/supplierAddress';

export const supplierAddressApi = {
  // Lấy danh sách địa chỉ theo ID nhà cung cấp
  getBySupplierId: (supplierId: number) => {
    return axiosClient.get(`/SupplierAddresses/supplier/${supplierId}`);
  },

  // Tạo mới địa chỉ cho nhà cung cấp (Đã sửa lại URL khớp với Backend)
  create: (supplierId: number, data: SupplierAddressPayload) => {
    return axiosClient.post(`/SupplierAddresses/${supplierId}`, data);
  },

  // Cập nhật địa chỉ
  update: (id: number, data: SupplierAddressPayload) => {
    return axiosClient.put(`/SupplierAddresses/${id}`, data);
  },

  // Xóa địa chỉ
  delete: (id: number) => {
    return axiosClient.delete(`/SupplierAddresses/${id}`);
  },

  // Đặt làm mặc định
  setDefault: (id: number, supplierId: number) => {
    return axiosClient.patch(`/SupplierAddresses/${id}/set-default/${supplierId}`);
  },
};
