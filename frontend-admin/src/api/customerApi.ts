import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { Customer, CustomerPayload, CustomerQueryParams } from '../types/customer';

export const customerApi = {
    //1. GET
    getAll: (params?: CustomerQueryParams): Promise<PagedResult<Customer>> => {
        return axiosClient.get('/Customers', { params });
    },

    getAllList: (): Promise<Customer[]> => {
        return axiosClient.get('/Customers/all');
    },

    //2. GET by ID
    getById: (id: number): Promise<Customer> => {
        return axiosClient.get(`/Customers/${id}`);
    },

    //3. POST
    create: (data: CustomerPayload): Promise<Customer> => {
        return axiosClient.post('/Customers', data);
    },

    //4. PUT
    update: (id: number, data: CustomerPayload): Promise<void> => {
        return axiosClient.put(`/Customers/${id}`, data);
    },

    //5. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/Customers/${id}`);
    },
};