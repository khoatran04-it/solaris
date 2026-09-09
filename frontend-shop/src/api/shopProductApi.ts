import axiosClient from "./axiosClient";
import {
  ShopProductCard,
  ShopProductDetail,
  ShopCategoryTree,
  ShopProductFilterParams,
} from "@/types/product";
import { ShopPromotionBadge } from "@/types/product";
import { ShopPromotionDetail } from "@/types/promotion";
import { PagedResult } from "@/types/common";

const shopProductApi = {
  getAll: (params?: ShopProductFilterParams) =>
    axiosClient.get<PagedResult<ShopProductCard>>("/products", { params }),

  getBySlug: (slug: string, warehouseId?: number) =>
    axiosClient.get<ShopProductDetail>(`/products/${slug}`, {
      params: warehouseId ? { warehouseId } : undefined,
    }),

  getCategories: () =>
    axiosClient.get<ShopCategoryTree[]>("/products/categories"),

  getFeatured: (limit: number = 8, warehouseId?: number) =>
    axiosClient.get<ShopProductCard[]>("/products/featured", {
      params: { limit, ...(warehouseId ? { warehouseId } : {}) },
    }),

  getNewArrivals: (limit: number = 8, warehouseId?: number) =>
    axiosClient.get<ShopProductCard[]>("/products/new-arrivals", {
      params: { limit, ...(warehouseId ? { warehouseId } : {}) },
    }),

  getOrigins: () => axiosClient.get<string[]>("/products/origins"),

  getCertifications: () =>
    axiosClient.get<string[]>("/products/certifications"),

  getPromotions: () =>
    axiosClient.get<ShopPromotionBadge[]>("/products/promotions"),

  getPromotionBySlug: (slug: string) =>
    axiosClient.get<ShopPromotionDetail>(`/products/promotions/${slug}`),
};

export default shopProductApi;
