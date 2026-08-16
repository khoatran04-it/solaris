import { PaginationParams } from './common';

// Phụ trợ: Thông tin biến thể hiển thị bên trong chi tiết chiến dịch
export interface CampaignAppliedVariant {
    variantId: number;
    variantCode: string;
    variantName: string;
    productName?: string;
    defaultPrice: number;
    defaultUoMName?: string; // 🔥 BỔ SUNG: Tên đơn vị tính mặc định (Ví dụ: Kg, Thùng...)
    imagePath?: string;
}

// 1. Dữ liệu đọc (Match với PromotionCampaignReadDto)
export interface PromotionCampaign {
    id: number;
    name: string;
    description?: string;
    isPercentage: boolean;
    discountValue: number;
    startDate: string;
    endDate: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;

    appliedVariants: CampaignAppliedVariant[];
}

// 2. Dữ liệu ghi (Match với PromotionCampaignCreateDto / PromotionCampaignUpdateDto)
export interface PromotionCampaignPayload {
    name: string;
    description?: string | null;
    isPercentage: boolean;
    discountValue: number;
    startDate: string;
    endDate: string;
    isActive: boolean;

    // Tùy chọn gửi kèm danh sách ID biến thể lúc tạo mới (Nếu update vỏ thì không truyền)
    variantIds?: number[]; 
}

// Payload riêng dùng cho việc gắn/gỡ sản phẩm hàng loạt (Match với ApplyVariantsToCampaignDto)
export interface ApplyVariantsToCampaignPayload {
    variantIds: number[];
}

// 3. Tham số truy vấn
export interface PromotionCampaignQueryParams extends PaginationParams {
    search?: string;
    isActive?: boolean;
    startDate?: string;
    endDate?: string;
}