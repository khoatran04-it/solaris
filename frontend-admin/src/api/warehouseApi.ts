import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    Warehouse, 
    WarehousePayload, 
    WarehouseQueryParams 
} from '../types/warehouse';

export const warehouseApi = {
    // 1. GET (Có phân trang & Lọc)
    getAll: (params?: WarehouseQueryParams): Promise<PagedResult<Warehouse>> => {
        return axiosClient.get('/warehouses', { params });
    },

    // 2. GET ALL (Lấy tất cả)
    getAllList: (isActiveOnly: boolean = false): Promise<Warehouse[]> => {
        return axiosClient.get('/warehouses/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    // 3. GET by ID (Lấy chi tiết kho hàng bao gồm Địa chỉ & Trưởng kho)
    getById: (id: number): Promise<Warehouse> => {
        return axiosClient.get(`/warehouses/${id}`);
    },

    // 4. POST (Thêm mới kho hàng & Địa chỉ bằng Transaction)
    create: (data: WarehousePayload): Promise<Warehouse> => {
        return axiosClient.post('/warehouses', data);
    },

    // 5. PUT (Cập nhật kho hàng & Ghi đè địa chỉ)
    update: (id: number, data: WarehousePayload): Promise<void> => {
        return axiosClient.put(`/warehouses/${id}`, data);
    },

    // 6. DELETE (Xóa mềm)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/warehouses/${id}`);
    },

    // 7. PATCH (Thay đổi trạng thái Tạm khóa / Hoạt động)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/warehouses/${id}/toggle-active`);
    }
};