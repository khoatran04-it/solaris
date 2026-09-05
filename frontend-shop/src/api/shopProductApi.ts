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

  getBySlug: (slug: string) =>
    axiosClient.get<ShopProductDetail>(`/products/${slug}`),

  getCategories: () =>
    axiosClient.get<ShopCategoryTree[]>("/products/categories"),

  getFeatured: (limit: number = 8) =>
    axiosClient.get<ShopProductCard[]>(`/products/featured?limit=${limit}`),

  getNewArrivals: (limit: number = 8) =>
    axiosClient.get<ShopProductCard[]>(`/products/new-arrivals?limit=${limit}`),

  getOrigins: () => axiosClient.get<string[]>("/products/origins"),

  getCertifications: () =>
    axiosClient.get<string[]>("/products/certifications"),

  getPromotions: () =>
    axiosClient.get<ShopPromotionBadge[]>("/products/promotions"),

  getPromotionBySlug: (slug: string) =>
    axiosClient.get<ShopPromotionDetail>(`/products/promotions/${slug}`),
};

export default shopProductApi;
