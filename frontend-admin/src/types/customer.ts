import { PaginationParams } from './common';
import { CustomerAddress, CustomerAddressPayload } from './customerAddress';

// 1. Dữ liệu đọc (Match với CustomerReadDto)
export interface Customer {
    id: number;
    code: string;
    name: string;
    phoneNumber: string;
    email?: string;
    taxCode?: string;
    avatarPath?: string;
    birthday?: string;
    gender?: boolean;
    note?: string;
    createdAt: string;
    updatedAt: string;
    isActive: boolean;

    customerTypeId: number;
    customerTypeName?: string; 
    customerTierId: number;
    customerTierName?: string; 
    groupIds: number[];
    groups: string[]; 
    addresses: CustomerAddress[];
}

// 2. Dữ liệu ghi (Match với CustomerUpdateDto / CustomerCreateDto)
export interface CustomerPayload {
    code: string;
    name: string;
    phoneNumber: string;
    email?: string | null;
    taxCode?: string | null;
    avatarPath?: string | null;
    birthday?: string | null; 
    gender?: boolean | null;
    note?: string | null;
    isActive: boolean;
    customerTypeId: number;
    customerTierId: number;

    groupIds: number[]; 
    
    // (Tùy chọn) Gửi kèm địa chỉ lúc tạo mới nếu cần
    addresses?: CustomerAddressPayload[]; 
}

// 3. Tham số truy vấn
export interface CustomerQueryParams extends PaginationParams {
    customerTypeId?: string;
    customerTierId?: string;
    customerGroupId?: string; 
    isActive?: boolean;
}