import axiosClient from './axiosClient';
import {PagedResult} from '../types/common';
import { Supplier, SupplierPayload, SupplierQueryParams } from "../types/supplier";

export const supplierApi = {
    //1. GET
    getAll:( params?: SupplierQueryParams): Promise<PagedResult<Supplier>> => {
        return axiosClient.get('/Suppliers', { params });
    },

    getAllList: (): Promise<Supplier[]> => {
        return axiosClient.get('/Suppliers/all');
    },

    //2. GET by ID
    getById: (id: number): Promise<Supplier> => {
        return axiosClient.get(`/Suppliers/${id}`);
    },

    //3. POST
    create: (data: SupplierPayload): Promise<Supplier> => {
        return axiosClient.post('/Suppliers', data);
    },

    //4. PUT
    update: (id: number, data: SupplierPayload): Promise<void> => {
        return axiosClient.put(`/Suppliers/${id}`, data);
    },

    //5. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/Suppliers/${id}`);
    },
    
    // 6. PATCH (Thay đổi trạng thái Tạm khóa / Hoạt động)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/Suppliers/${id}/toggle-active`);
    }
    
}