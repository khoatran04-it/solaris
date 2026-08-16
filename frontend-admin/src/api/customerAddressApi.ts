import axiosClient from './axiosClient';
import { CustomerAddress, CustomerAddressPayload } from '../types/customerAddress';

export const customerAddressApi = {
    getByCustomerId: (customerId: number): Promise<CustomerAddress[]> => {
        return axiosClient.get(`/CustomerAddresses/customer/${customerId}`);
    },

    getById: (id: number): Promise<CustomerAddress> => {
        return axiosClient.get(`/CustomerAddresses/${id}`);
    },

    create: (customerId: number, data: CustomerAddressPayload): Promise<CustomerAddress> => {
        return axiosClient.post(`/CustomerAddresses/customer/${customerId}`, data);
    },

    update: (id: number, data: CustomerAddressPayload): Promise<void> => {
        return axiosClient.put(`/CustomerAddresses/${id}`, data);
    },

    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/CustomerAddresses/${id}`);
    },

    setDefault: (id: number, customerId: number): Promise<void> => {
        return axiosClient.patch(`/CustomerAddresses/${id}/set-default`, { customerId });
    }
};