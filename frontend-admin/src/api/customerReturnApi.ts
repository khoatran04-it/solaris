import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  CustomerReturn,
  CustomerReturnCreatePayload,
  CustomerReturnInspectionPayload,
  CustomerReturnQueryParams,
} from '../types/customerReturn';

export const customerReturnApi = {
  getAll: (params?: CustomerReturnQueryParams): Promise<PagedResult<CustomerReturn>> => {
    return axiosClient.get('/CustomerReturns', { params });
  },

  getById: (id: number): Promise<CustomerReturn> => {
    return axiosClient.get(`/CustomerReturns/${id}`);
  },

  create: (data: CustomerReturnCreatePayload): Promise<{ id: number; message?: string }> => {
    return axiosClient.post('/CustomerReturns', data);
  },

  approve: (id: number): Promise<{ message?: string }> => {
    return axiosClient.post(`/CustomerReturns/${id}/approve`);
  },

  inspect: (id: number, data: CustomerReturnInspectionPayload): Promise<{ message?: string }> => {
    return axiosClient.post(`/CustomerReturns/${id}/inspect`, data);
  },

  complete: (id: number): Promise<{ message?: string }> => {
    return axiosClient.post(`/CustomerReturns/${id}/complete`);
  },

  inspectAndComplete: (
    id: number,
    data: CustomerReturnInspectionPayload
  ): Promise<{ message?: string }> => {
    return axiosClient.post(`/CustomerReturns/${id}/inspect-and-complete`, data);
  },

  reject: (id: number, reason: string): Promise<{ message?: string }> => {
    return axiosClient.post(`/CustomerReturns/${id}/reject`, { reason });
  },

  delete: (id: number): Promise<{ message?: string }> => {
    return axiosClient.delete(`/CustomerReturns/${id}`);
  },
};
