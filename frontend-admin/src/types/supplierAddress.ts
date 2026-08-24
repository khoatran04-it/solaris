export interface SupplierAddress {
  id: number;
  supplierId: number;
  contactName: string;
  contactPhone: string;
  province: string;
  district: string;
  ward: string;
  streetAddress: string;
  fullAddress: string;
  isDefault: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SupplierAddressPayload {
  contactName: string;
  contactPhone: string;
  province: string;
  district: string;
  ward: string;
  streetAddress: string;
  isDefault: boolean;
}
