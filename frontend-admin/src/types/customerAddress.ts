export interface CustomerAddress {
    id: number;
    customerId: number;
    receiverName: string;
    phone: string;
    province: string;
    district: string;
    ward: string;
    streetAddress: string;
    fullAddress: string;
    latitude?: number;
    longitude?: number;
    isDefault: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface CustomerAddressPayload {
    receiverName: string;
    phone: string;
    province: string;
    district: string;
    ward: string;
    streetAddress: string;
    latitude?: number;
    longitude?: number;
    isDefault: boolean;
}