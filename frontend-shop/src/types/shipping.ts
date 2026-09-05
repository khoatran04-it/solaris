export interface GhnProvince {
  provinceID: number;
  provinceName: string;
  code: string;
}

export interface GhnDistrict {
  districtID: number;
  provinceID: number;
  districtName: string;
  code: string;
}

export interface GhnWard {
  wardCode: string;
  districtID: number;
  wardName: string;
}

export interface GhnCalculateFeePayload {
  toDistrictId: number;
  toWardCode: string;
  weightGram?: number;
  subTotal: number;
}

export interface GhnCalculateFeeResponse {
  totalFee: number;
  originalFee: number;
  isFreeShipping: boolean;
  freeShippingThreshold: number;
  amountNeededForFreeShipping: number;
  expectedDeliveryTime?: string;
}
