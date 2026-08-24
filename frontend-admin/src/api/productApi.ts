import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  Product,
  ProductPayload,
  ProductQueryParams,
  ProductAttributeConfig,
} from '../types/product';

export const productApi = {
  // 1. GET (Phân trang & Lọc)
  getAll: (params?: ProductQueryParams): Promise<PagedResult<Product>> => {
    return axiosClient.get('/Products', { params });
  },

  // 2. GET ALL (Không phân trang)
  getAllList: (isActiveOnly: boolean = false): Promise<Product[]> => {
    return axiosClient.get('/Products/all', {
      params: { isActive: isActiveOnly ? true : undefined },
    });
  },

  // 3. GET by ID
  getById: (id: number): Promise<Product> => {
    return axiosClient.get(`/Products/${id}`);
  },

  // 4. POST
  create: (data: ProductPayload): Promise<Product> => {
    return axiosClient.post('/Products', data);
  },

  // 5. PUT
  update: (id: number, data: ProductPayload): Promise<void> => {
    return axiosClient.put(`/Products/${id}`, data);
  },

  // 6. DELETE
  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/Products/${id}`);
  },

  // 7. PATCH (Thay đổi trạng thái Đang bán / Ngừng bán)
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/Products/${id}/toggle-active`);
  },

  // 8. Lấy cấu hình thuộc tính động (Dùng cho trang tạo ProductVariant)
  getAttributesConfig: (id: number): Promise<ProductAttributeConfig[]> => {
    return axiosClient.get(`/Products/${id}/attributes-config`);
  },
};
