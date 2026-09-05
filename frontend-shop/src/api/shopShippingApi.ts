import axiosClient from "./axiosClient";
import {
  GhnProvince,
  GhnDistrict,
  GhnWard,
  GhnCalculateFeePayload,
  GhnCalculateFeeResponse,
} from "@/types/shipping";

const shopShippingApi = {
  getProvinces: () => axiosClient.get<GhnProvince[]>("/shipping/provinces"),

  getDistricts: (provinceId: number) =>
    axiosClient.get<GhnDistrict[]>(`/shipping/districts/${provinceId}`),

  getWards: (districtId: number) =>
    axiosClient.get<GhnWard[]>(`/shipping/wards/${districtId}`),

  calculateFee: (payload: GhnCalculateFeePayload) =>
    axiosClient.post<GhnCalculateFeeResponse>(
      "/shipping/calculate-fee",
      payload,
    ),
};

export default shopShippingApi;
