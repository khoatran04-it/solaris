import { PaginationParams } from './common';

// =========================================================
// CÁC TYPE PHỤ TRỢ (Cho Thuộc tính & Bảng giá)
// =========================================================

// 1. Phụ trợ: Thông tin thuộc tính hiển thị (Lúc get về)
export interface VariantAttribute {
    id: number;
    attributeDefinitionId?: number;
    attributeDefinitionName?: string;
    attributeValue: string;
}

// 2. Phụ trợ: Dữ liệu thuộc tính gửi lên (Lúc Create/Update)
export interface AttributeInput {
    attributeDefinitionId: number;
    attributeValue: string;
}

// 🔥 3. Phụ trợ (MỚI): Bảng giá hiển thị (Match VariantPriceReadDto)
export interface VariantPrice {
    id: number;
    uoMId: number;
    uoMName?: string;
    price: number; // Giá niêm yết
    promotionalPrice?: number | null; // Giá KM tính on-the-fly
    isDefault: boolean;
}

// 🔥 4. Phụ trợ (MỚI): Dữ liệu Bảng giá gửi lên (Match VariantPriceInputDto)
export interface VariantPriceInput {
    uoMId: number;
    price: number;
    isDefault: boolean;
}

// =========================================================
// TYPE CHÍNH CỦA BIẾN THỂ
// =========================================================

// 5. Dữ liệu đọc (Match với ProductVariantReadDto)
export interface ProductVariant {
    id: number;
    code: string;
    name: string;
    description?: string;
    imagePath?: string;
    
    // 🔥 ĐÃ XÓA: defaultPrice và promotionalPrice ở gốc
    
    inventoryGuideline: number;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
    productId: number;
    productName: string;
    
    // Ngăn chứa dữ liệu con
    attributes: VariantAttribute[]; 
    prices: VariantPrice[]; // 🔥 BỔ SUNG: Mảng quy cách bán hàng
}

// 6. Dữ liệu ghi (Match với ProductVariantCreateDto / ProductVariantUpdateDto)
export interface ProductVariantPayload {
    code: string;
    name: string;
    description?: string | null;
    imagePath?: string | null;
    
    // 🔥 ĐÃ XÓA: defaultPrice ở gốc

    inventoryGuideline: number;
    isActive: boolean;
    productId: number;
    
    // Mảng dữ liệu con (Gửi lên 1 cục để BE xử lý Transaction)
    attributes: AttributeInput[]; 
    prices: VariantPriceInput[]; // 🔥 BỔ SUNG: Mảng quy cách bán hàng
}

// 7. Tham số truy vấn (Lọc danh sách)
export interface ProductVariantQueryParams extends PaginationParams {
    search?: string; // Tìm theo Tên, Mã
    isActive?: boolean;
    productId?: string; // Dùng chuỗi để có thể lọc "1,2,3" (nếu chọn nhiều Product gốc)
    createdAt?: string;
    updatedAt?: string;
}