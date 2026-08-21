import axiosClient from './axiosClient';
import { ShopReturn, ShopReturnCreatePayload } from '@/types/return';
import { PagedResult } from '@/types/common';

const shopReturnApi = {
    create: (data: ShopReturnCreatePayload) =>
        axiosClient.post<ShopReturn>('/returns', data),

    getAll: (pageIndex: number = 1, pageSize: number = 10) =>
        axiosClient.get<PagedResult<ShopReturn>>('/returns', { params: { pageIndex, pageSize } }),

    getByCode: (returnCode: string) =>
        axiosClient.get<ShopReturn>(`/returns/${returnCode}`),
};

export default shopReturnApi;
