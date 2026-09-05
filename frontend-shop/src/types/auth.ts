export interface ShopCustomerInfo {
  id: number;
  code: string;
  name: string;
  phoneNumber: string;
  email?: string;
  avatarPath?: string;
  customerTierId?: number;
  customerTierName?: string;
  discountPercent: number;
}

export interface ShopAuthResponse {
  token: string;
  customerInfo: ShopCustomerInfo;
}

export interface ShopLoginPayload {
  username: string;
  password: string;
}

export interface ShopRegisterPayload {
  fullName: string;
  phoneNumber: string;
  email?: string;
  password: string;
  province?: string;
  district?: string;
  ward?: string;
  streetAddress?: string;
  latitude?: number;
  longitude?: number;
}
