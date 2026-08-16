import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    SupplierProduct, 
    SupplierProductPayload, 
    SupplierProductQueryParams 
} from '../types/supplierProduct';

export const supplierProductApi = {
    getAll: (params?: SupplierProductQueryParams): Promise<PagedResult<SupplierProduct>> => {
        return axiosClient.get('/SupplierProducts', { params });
    },

    getAllList: (): Promise<SupplierProduct[]> => {
        return axiosClient.get('/SupplierProducts/all');
    },

    getById: (id: number): Promise<SupplierProduct> => {
        return axiosClient.get(`/SupplierProducts/${id}`);
    },

    create: (data: SupplierProductPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/SupplierProducts', data);
    },

    update: (id: number, data: SupplierProductPayload): Promise<void> => {
        return axiosClient.put(`/SupplierProducts/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/SupplierProducts/${id}`);
    },
};