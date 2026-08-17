import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    SupplierProduct, 
    SupplierProductPayload, 
    SupplierProductQueryParams 
} from '../types/supplierProduct';

export const supplierProductApi = {
    // 1. GET (Phân trang và lọc)
    getAll: (params?: SupplierProductQueryParams): Promise<PagedResult<SupplierProduct>> => {
        return axiosClient.get('/SupplierProducts', { params });
    },

    // 2. GET ALL (Toàn bộ)
    getAllList: (isActiveOnly: boolean = false): Promise<SupplierProduct[]> => {
        return axiosClient.get('/SupplierProducts/all', { params: { isActive: isActiveOnly ? true : undefined } });
    },

    // 3. GET BY SUPPLIER ID (Lấy tất cả sản phẩm của 1 NCC)
    getBySupplierId: (supplierId: number, isActiveOnly: boolean = true): Promise<SupplierProduct[]> => {
        return axiosClient.get(`/SupplierProducts/by-supplier/${supplierId}`, { params: { isActiveOnly } });
    },

    // 4. GET BY ID
    getById: (id: number): Promise<SupplierProduct> => {
        return axiosClient.get(`/SupplierProducts/${id}`);
    },

    // 5. POST (Tạo mới)
    create: (data: SupplierProductPayload): Promise<SupplierProduct> => {
        return axiosClient.post('/SupplierProducts', data);
    },

    // 6. PUT (Cập nhật)
    update: (id: number, data: SupplierProductPayload): Promise<void> => {
        return axiosClient.put(`/SupplierProducts/${id}`, data);
    },

    // 7. DELETE (Xóa mềm)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/SupplierProducts/${id}`);
    },

    // 8. TOGGLE ACTIVE (Bật/Tắt trạng thái)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/SupplierProducts/${id}/toggle-active`);
    }
};