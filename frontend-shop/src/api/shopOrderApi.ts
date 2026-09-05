import axiosClient from './axiosClient';
import { ShopOrder, ShopCheckoutPayload, ShopOrderCancelPayload } from '@/types/order';
import { PagedResult } from '@/types/common';

const shopOrderApi = {
    checkout: (data: ShopCheckoutPayload) =>
        axiosClient.post<ShopOrder>('/orders/checkout', data),

    getAll: (pageIndex: number = 1, pageSize: number = 10) =>
        axiosClient.get<PagedResult<ShopOrder>>('/orders', { params: { pageIndex, pageSize } }),

    getByCode: (orderCode: string) =>
        axiosClient.get<ShopOrder>(`/orders/${orderCode}`),

    cancel: (orderCode: string, data: ShopOrderCancelPayload) =>
        axiosClient.post(`/orders/${orderCode}/cancel`, data),

    confirmDelivery: (orderCode: string) =>
        axiosClient.post<ShopOrder>(`/orders/${orderCode}/confirm-delivery`),
};

export default shopOrderApi;
