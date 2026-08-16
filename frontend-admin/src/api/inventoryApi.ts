import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    Inventory, 
    InventoryQueryParams 
} from '../types/inventory';

export const inventoryApi = {
    // 1. GET (Có phân trang & Lọc - Dùng cho bảng danh sách Tồn kho)
    getAll: (params?: InventoryQueryParams): Promise<PagedResult<Inventory>> => {
        return axiosClient.get('/inventories', { params });
    },

    // 2. GET ALL (Lấy tất cả - Dùng khi sếp làm tính năng Xuất file Excel toàn bộ tồn kho)
    getAllList: (): Promise<Inventory[]> => {
        return axiosClient.get('/inventories/all');
    },

    // 3. GET by ID (Lấy chi tiết 1 dòng tồn kho)
    getById: (id: number): Promise<Inventory> => {
        return axiosClient.get(`/inventories/${id}`);
    }

    // 💡 LƯU Ý KỸ THUẬT: 
    // Không có các hàm Create, Update, Delete ở đây. 
    // Số liệu Tồn kho CHỈ ĐƯỢC PHÉP thay đổi thông qua các module Chứng từ Nhập/Xuất/Điều chỉnh (Phase 3 & 4).
};