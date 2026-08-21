import axiosClient from './axiosClient';
import { ShopAuthResponse, ShopLoginPayload, ShopRegisterPayload, ShopCustomerInfo } from '@/types/auth';

const shopAuthApi = {
    register: (data: ShopRegisterPayload) =>
        axiosClient.post<ShopAuthResponse>('/auth/register', data),

    login: (data: ShopLoginPayload) =>
        axiosClient.post<ShopAuthResponse>('/auth/login', data),

    getMe: () =>
        axiosClient.get<ShopCustomerInfo>('/auth/me'),
};

export default shopAuthApi;
