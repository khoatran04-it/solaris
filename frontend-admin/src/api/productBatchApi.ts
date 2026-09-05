import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { ProductBatch, ProductBatchPayload, ProductBatchQueryParams } from '../types/productBatch';

export const productBatchApi = {
  getAll: (params?: ProductBatchQueryParams): Promise<PagedResult<ProductBatch>> => {
    return axiosClient.get('/ProductBatches', { params });
  },

  getAllList: (): Promise<ProductBatch[]> => {
    return axiosClient.get('/ProductBatches/all');
  },

  getById: (id: number): Promise<ProductBatch> => {
    return axiosClient.get(`/ProductBatches/${id}`);
  },

  // Ánh xạ trường Id từ API response
  create: (
    data: ProductBatchPayload
  ): Promise<{ message?: string; Message?: string; id?: number; Id?: number }> => {
    return axiosClient.post('/ProductBatches', data);
  },

  update: (id: number, data: ProductBatchPayload): Promise<void> => {
    return axiosClient.put(`/ProductBatches/${id}`, data);
  },

  delete: (id: number): Promise<void> => {
    return axiosClient.delete(`/ProductBatches/${id}`);
  },
};
