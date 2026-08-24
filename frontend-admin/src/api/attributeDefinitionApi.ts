import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  AttributeDefinition,
  AttributeDefinitionPayload,
  AttributeDefinitionQueryParams,
} from '../types/attributeDefinition';

export const attributeDefinitionApi = {
  // 1. GET (Phân trang & Lọc)
  getAll: (params?: AttributeDefinitionQueryParams): Promise<PagedResult<AttributeDefinition>> => {
    return axiosClient.get('/AttributeDefinitions', { params });
  },

  // 2. GET ALL (Không phân trang)
  getAllList: (isActiveOnly: boolean = false): Promise<AttributeDefinition[]> => {
    return axiosClient.get('/AttributeDefinitions/all', {
      params: { isActive: isActiveOnly ? true : undefined },
    });
  },

  // 3. GET by ID
  getById: (id: number): Promise<AttributeDefinition> => {
    return axiosClient.get(`/AttributeDefinitions/${id}`);
  },

  // 4. POST
  create: (data: AttributeDefinitionPayload): Promise<AttributeDefinition> => {
    return axiosClient.post('/AttributeDefinitions', data);
  },

  // 5. PUT
  update: (id: number, data: AttributeDefinitionPayload): Promise<void> => {
    return axiosClient.put(`/AttributeDefinitions/${id}`, data);
  },

  // 6. DELETE
  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/AttributeDefinitions/${id}`);
  },

  // 7. PATCH (Thay đổi trạng thái Tạm khóa / Hoạt động)
  toggleActive: (id: number): Promise<void> => {
    return axiosClient.patch(`/AttributeDefinitions/${id}/toggle-active`);
  },
};
