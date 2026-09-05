import { ShopCustomerInfo } from "./auth";

export interface ShopCustomerProfile extends ShopCustomerInfo {
  birthday?: string;
  gender?: boolean | null;
  totalSpent?: number;
  totalOrders?: number;
  nextTierName?: string;
  nextTierMinSpending?: number;
  amountToNextTier?: number;
  tierProgressPercent?: number;
  addresses: ShopAddress[];
}

export interface ShopCustomerProfileUpdatePayload {
  name?: string;
  phoneNumber?: string;
  email?: string;
  birthday?: string;
  gender?: boolean | null;
  avatarPath?: string;
}

export interface ShopAddress {
  id: number;
  receiverName: string;
  phone: string;
  province: string;
  district: string;
  ward: string;
  streetAddress: string;
  fullAddress: string;
  isDefault: boolean;
  latitude: number;
  longitude: number;
}

export interface ShopAddressPayload {
  receiverName: string;
  phone: string;
  province: string;
  district: string;
  ward: string;
  streetAddress: string;
  isDefault?: boolean;
  latitude?: number;
  longitude?: number;
}
