import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { UoMCategory, UoMCategoryPayload, UoMCategoryQueryParams } from '../types/uomCategory';

export const uomCategoryApi = {
  // 1. GET Paged (Dành cho bảng danh sách có phân trang và filter)
  getAll: (params?: UoMCategoryQueryParams): Promise<PagedResult<UoMCategory>> => {
    return axiosClient.get('/UoMCategories', { params });
  },

  // 2. GET ALL (Không phân trang - Dùng cho Dropdown Select ở form UoM)
  getAllList: (isActiveOnly: boolean = false): Promise<UoMCategory[]> => {
    return axiosClient.get('/UoMCategories/all', {
      params: { isActive: isActiveOnly ? true : undefined },
    });
  },

  // 3. GET by ID (Dùng khi mở form Edit để fetch dữ liệu cũ)
  getById: (id: number): Promise<UoMCategory> => {
    return axiosClient.get(`/UoMCategories/${id}`);
  },

  // 4. POST (Thêm mới)
  create: (data: UoMCategoryPayload): Promise<UoMCategory> => {
    return axiosClient.post('/UoMCategories', data);
  },

  // 5. PUT (Cập nhật toàn bộ)
  update: (id: number, data: UoMCategoryPayload): Promise<void> => {
    return axiosClient.put(`/UoMCategories/${id}`, data);
  },

  // 6. DELETE (Xóa)
  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/UoMCategories/${id}`);
  },

  // 7. PATCH (Thay đổi trạng thái Hoạt động / Tạm khóa)
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/UoMCategories/${id}/toggle-active`);
  },
};
