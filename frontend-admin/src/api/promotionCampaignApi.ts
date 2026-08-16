import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import { 
    PromotionCampaign, 
    PromotionCampaignPayload, 
    PromotionCampaignQueryParams,
    ApplyVariantsToCampaignPayload
} from '../types/promotionCampaign';

export const promotionCampaignApi = {
    // 1. GET (Phân trang & Lọc)
    getAll: (params?: PromotionCampaignQueryParams): Promise<PagedResult<PromotionCampaign>> => {
        return axiosClient.get('/PromotionCampaigns', { params });
    },

    // GET ALL (Lấy tất cả không phân trang)
    getAllList: (): Promise<PromotionCampaign[]> => {
        return axiosClient.get('/PromotionCampaigns/all');
    },

    // 2. GET by ID
    getById: (id: number): Promise<PromotionCampaign> => {
        return axiosClient.get(`/PromotionCampaigns/${id}`);
    },

    // 3. POST (Tạo vỏ chiến dịch - Backend trả về kèm Id mới tạo)
    create: (data: PromotionCampaignPayload): Promise<{ message: string; id: number }> => {
        return axiosClient.post('/PromotionCampaigns', data);
    },

    // 4. PUT (Cập nhật vỏ chiến dịch)
    update: (id: number, data: PromotionCampaignPayload): Promise<void> => {
        return axiosClient.put(`/PromotionCampaigns/${id}`, data);
    },

    // 5. DELETE
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/PromotionCampaigns/${id}`);
    },

    // 6. PATCH (Bật/Tắt trạng thái)
    toggleActive: (id: number): Promise<void> => {
        return axiosClient.patch(`/PromotionCampaigns/${id}/toggle-active`);
    },

    // 7. ACTION POST: Gắn hàng loạt sản phẩm vào chiến dịch
    addVariants: (id: number, data: ApplyVariantsToCampaignPayload): Promise<void> => {
        return axiosClient.post(`/PromotionCampaigns/${id}/add-variants`, data);
    },

    // 8. ACTION POST: Gỡ hàng loạt sản phẩm khỏi chiến dịch
    removeVariants: (id: number, data: ApplyVariantsToCampaignPayload): Promise<void> => {
        return axiosClient.post(`/PromotionCampaigns/${id}/remove-variants`, data);
    }
};