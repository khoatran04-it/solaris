import { PaginationParams } from './common';

// 1. Model chính (Dùng để hứng dữ liệu Read từ Backend - InventoryReadDto)
export interface Inventory {
    id: number;

    // --- Tọa độ Kho ---
    warehouseId: number;
    warehouseName: string;
    warehouseCode: string;

    // --- Thông tin Sản phẩm (Biến thể) ---
    variantId: number;
    variantCode: string;
    variantName: string;
    baseUoMName: string; // Đơn vị tính cơ bản (Kg, Bó, Hộp...)

    // --- Thông tin Lô hàng & Nhà cung cấp ---
    batchId: number;
    batchCode: string;
    manufactureDate: string; // Ngày sản xuất (ISO 8601 string từ Backend)
    expiryDate: string;      // Hạn sử dụng (ISO 8601 string)
    daysToExpiry: number;    // Số ngày còn lại (Dùng để bôi màu Đỏ/Vàng/Xanh)
    supplierName: string;

    // --- Số liệu Tồn kho ---
    quantityAvailable: number; // Hàng bán được
    quantityReserved: number;  // Hàng giữ chỗ
    quantityQC: number;        // Hàng chờ kiểm tra
    quantityDamaged: number;   // Hàng hỏng
    
    // Tổng tồn kho vật lý
    totalQuantity: number;
}

// 2. Query Params (Dùng cho bộ lọc tìm kiếm trên Bảng Danh Sách)
export interface InventoryQueryParams extends PaginationParams {
    // search?: string; // Đã có sẵn trong PaginationParams
    warehouseId?: number | string; // Lọc theo Kho hàng
    isExpiringSoon?: boolean;      // Nút toggle: Chỉ hiện hàng sắp hết hạn
    isOutOfStock?: boolean;        // Nút toggle: Chỉ hiện hàng đã hết (Available = 0)
}