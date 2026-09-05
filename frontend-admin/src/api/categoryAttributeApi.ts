import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  CategoryAttribute,
  CategoryAttributePayload,
  CategoryAttributeQueryParams,
} from '../types/categoryAttribute';

export const categoryAttributeApi = {
  getAll: (params?: CategoryAttributeQueryParams): Promise<PagedResult<CategoryAttribute>> => {
    return axiosClient.get('/CategoryAttributes', { params });
  },

  getAllList: (): Promise<CategoryAttribute[]> => {
    return axiosClient.get('/CategoryAttributes/all');
  },

  getById: (id: number): Promise<CategoryAttribute> => {
    return axiosClient.get(`/CategoryAttributes/${id}`);
  },

  getByCategoryId: (categoryId: number): Promise<CategoryAttribute[]> => {
    return axiosClient.get(`/CategoryAttributes/category/${categoryId}`);
  },

  sync: (data: { categoryId: number; attributes: { attributeDefinitionId: number; isRequired: boolean }[] }): Promise<{ message: string }> => {
    return axiosClient.post('/CategoryAttributes/sync', data);
  },

  create: (data: CategoryAttributePayload): Promise<{ message: string; id: number }> => {
    return axiosClient.post('/CategoryAttributes', data);
  },

  update: (id: number, data: CategoryAttributePayload): Promise<void> => {
    return axiosClient.put(`/CategoryAttributes/${id}`, data);
  },

  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/CategoryAttributes/${id}`);
  },
};
