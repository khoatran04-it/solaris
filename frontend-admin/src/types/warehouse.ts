import { PaginationParams } from './common';

// 1. Payload Phụ trợ cho Địa chỉ kho (Dùng bên trong Create/Update)
export interface WarehouseAddressPayload {
    province: string;
    district: string;
    ward: string;
    streetAddress: string;
    latitude: number;
    longitude: number;
}

// 2. Model chính (Dùng để hứng dữ liệu Read từ Backend - WarehouseReadDto)
export interface Warehouse {
    id: number;
    code: string;
    name: string;
    warehouseType?: string;
    isActive: boolean;
    createdAt: string;

    // Thông tin Trưởng kho
    managerId?: number;
    managerName?: string;

    // Thông tin Địa chỉ (Đã được làm phẳng - Flatten từ Backend)
    addressId: number;
    province: string;
    district: string;
    ward: string;
    streetAddress: string;
    fullAddress: string; // Cột hiển thị nhanh
    latitude: number;
    longitude: number;
}

// 3. Payload (Dùng chung cho Form Create và Update)
export interface WarehousePayload {
    code?: string; // Bắt buộc khi Create, khi Update có thể bỏ qua
    name: string;
    warehouseType?: string;
    managerId?: number | null;
    isActive: boolean;
    address: WarehouseAddressPayload; // Khối địa chỉ lồng bên trong
}

// 4. Query Params (Dùng cho bộ lọc tìm kiếm trên Bảng Danh Sách)
export interface WarehouseQueryParams extends PaginationParams {
    isActive?: boolean | string | number;
    province?: string;
}