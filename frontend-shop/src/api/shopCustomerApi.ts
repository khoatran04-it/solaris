import axiosClient from "./axiosClient";
import {
  ShopCustomerProfile,
  ShopCustomerProfileUpdatePayload,
  ShopAddress,
  ShopAddressPayload,
} from "@/types/customer";

const shopCustomerApi = {
  getProfile: () => axiosClient.get<ShopCustomerProfile>("/customer/profile"),

  updateProfile: (data: ShopCustomerProfileUpdatePayload) =>
    axiosClient.put<ShopCustomerProfile>("/customer/profile", data),

  getAddresses: () => axiosClient.get<ShopAddress[]>("/customer/addresses"),

  createAddress: (data: ShopAddressPayload) =>
    axiosClient.post<ShopAddress>("/customer/addresses", data),

  updateAddress: (id: number, data: ShopAddressPayload) =>
    axiosClient.put<ShopAddress>(`/customer/addresses/${id}`, data),

  deleteAddress: (id: number) =>
    axiosClient.delete(`/customer/addresses/${id}`),

  setDefaultAddress: (id: number) =>
    axiosClient.post(`/customer/addresses/${id}/default`),
};

export default shopCustomerApi;
