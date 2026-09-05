import { ShopProductCard, ShopPromotionBadge } from "./product";

export type { ShopPromotionBadge };

export interface ShopPromotionDetail {
  id: number;
  name: string;
  slug: string;
  description?: string;
  bannerImagePath?: string;
  isPercentage: boolean;
  discountValue: number;
  startDate: string;
  endDate: string;
  products: ShopProductCard[];
}
