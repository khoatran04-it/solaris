import axiosClient from './axiosClient';
import { VnPayPaymentPayload, VnPayPaymentResponse, VnPayCallbackResult } from '@/types/payment';

const shopPaymentApi = {
    createVnPayUrl: (payload: VnPayPaymentPayload) =>
        axiosClient.post<VnPayPaymentResponse>('/payment/vnpay/create-url', payload),

    getVnPayCallback: (queryString: string) =>
        axiosClient.get<VnPayCallbackResult>(`/payment/vnpay/callback?${queryString}`),
};

export default shopPaymentApi;
