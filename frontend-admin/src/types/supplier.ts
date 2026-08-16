import { PaginationParams } from './common';
import { SupplierAddress, SupplierAddressPayload } from './supplierAddress'

export interface SupplierQueryParams extends PaginationParams {
    supplierTypesId?: string;
    isActive?: boolean;
}

//Supplier
export interface Supplier {
    id: number;
    code: string;
    name: string;
    logoPath?: string;
    phone: string;
    email: string;
    taxCode?: string;
    website?: string;
    socialLink?: string;
    bankAccount?: string;
    bankName?: string;
    note?: string;

    createdAt: string; // ISO date string
    updatedAt: string; // ISO date string
    isActive: boolean;

    supplierTypeId: number;
    supplierTypeName: string;

    addresses?: SupplierAddress[];
}

export interface SupplierPayload {
    code: string;
    name: string;
    logoPath?: string | null;
    phone: string;
    email: string;
    taxCode?: string | null;
    website?: string | null;
    socialLink?: string | null;
    bankAccount?: string | null;
    bankName?: string | null;
    note?: string | null;

    isActive: boolean;
    supplierTypeId: number
    addresses?: SupplierAddressPayload[]
}