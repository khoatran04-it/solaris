import axiosClient from './axiosClient';
import {
  GhnProvince,
  GhnDistrict,
  GhnWard,
  GhnCalculateFeePayload,
  GhnCalculateFeeResponse,
  GhnCreateOrderResponse,
} from '../types/shipping';

export const shippingApi = {
  getProvinces: (): Promise<GhnProvince[]> => axiosClient.get('/shipping/provinces'),

  getDistricts: (provinceId: number): Promise<GhnDistrict[]> =>
    axiosClient.get(`/shipping/districts/${provinceId}`),

  getWards: (districtId: number): Promise<GhnWard[]> =>
    axiosClient.get(`/shipping/wards/${districtId}`),

  calculateFee: (payload: GhnCalculateFeePayload): Promise<GhnCalculateFeeResponse> =>
    axiosClient.post('/shipping/calculate-fee', payload),

  createGhnOrder: (orderId: number): Promise<GhnCreateOrderResponse> =>
    axiosClient.post(`/shipping/ghn/create-order/${orderId}`),
};
