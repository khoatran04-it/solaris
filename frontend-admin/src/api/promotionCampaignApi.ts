import axiosClient from './axiosClient';
import { PagedResult } from '../types/common';
import {
  PromotionCampaign,
  PromotionCampaignPayload,
  PromotionCampaignQueryParams,
  ApplyVariantsToCampaignPayload,
} from '../types/promotionCampaign';

export const promotionCampaignApi = {
  // 1. GET (Phân trang & Lọc)
  getAll: (params?: PromotionCampaignQueryParams): Promise<PagedResult<PromotionCampaign>> => {
    return axiosClient.get('/PromotionCampaigns', { params });
  },

  // GET ALL (Lấy tất cả không phân trang)
  getAllList: (isActiveOnly: boolean = false): Promise<PromotionCampaign[]> => {
    return axiosClient.get('/PromotionCampaigns/all', {
      params: { isActive: isActiveOnly ? true : undefined },
    });
  },

  // 2. GET by ID
  getById: (id: number): Promise<PromotionCampaign> => {
    return axiosClient.get(`/PromotionCampaigns/${id}`);
  },

  // 3. POST
  create: (data: PromotionCampaignPayload): Promise<PromotionCampaign> => {
    return axiosClient.post('/PromotionCampaigns', data);
  },

  // 4. PUT
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

  // 7. ACTION POST: Gắn / Đồng bộ hàng loạt sản phẩm vào chiến dịch
  addVariants: (id: number, data: ApplyVariantsToCampaignPayload): Promise<void> => {
    return axiosClient.post(`/PromotionCampaigns/${id}/add-variants`, data);
  },

  // 8. ACTION POST: Gỡ hàng loạt sản phẩm khỏi chiến dịch
  removeVariants: (id: number, data: ApplyVariantsToCampaignPayload): Promise<void> => {
    return axiosClient.post(`/PromotionCampaigns/${id}/remove-variants`, data);
  },
};
