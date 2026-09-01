export interface ShopPromotionBadge {
    id: number;
    name: string;
    slug: string;
    isPercentage: boolean;
    discountValue: number;
    endDate: string;
}

export interface ShopProductCard {
    id: number;
    code: string;
    name: string;
    slug: string;
    imagePath?: string;
    categoryId?: number;
    categoryName?: string;
    categorySlug?: string;
    categoryGroupName?: string;
    categoryGroupSlug?: string;
    baseUoMName: string;
    minPrice: number;
    maxPrice: number;
    originalPrice: number;
    discountedPrice: number;
    discountPercent: number;
    hasPromotion: boolean;
    promotionName?: string;
    origin?: string;
    certification?: string;
    brixLevel?: string;
    isInStock: boolean;
    totalAvailableStock: number;
}

export interface ShopVariantPrice {
    priceId: number;
    uoMId: number;
    uoMName: string;
    price: number;
    discountedPrice: number;
    discountPercent: number;
    isDefault: boolean;
    conversionFactor?: number;
    conversionText?: string;
}

export interface ShopProductVariant {
    id: number;
    code: string;
    name: string;
    description?: string;
    imagePath?: string;
    prices: ShopVariantPrice[];
    attributes: Record<string, string>;
    quantityAvailable: number;
    isInStock: boolean;
}

export interface ShopProductDetail {
    id: number;
    code: string;
    name: string;
    slug: string;
    description?: string;
    imagePath?: string;
    categoryId?: number;
    categoryName?: string;
    categorySlug?: string;
    categoryGroupName?: string;
    categoryGroupSlug?: string;
    baseUoMId: number;
    baseUoMName: string;
    attributes: Record<string, string>;
    variants: ShopProductVariant[];
    activePromotions: ShopPromotionBadge[];
}

export interface ShopCategoryItem {
    categoryId: number;
    categoryName: string;
    categorySlug: string;
    categoryImage?: string;
    productCount: number;
}

export interface ShopCategoryTree {
    groupId: number;
    groupName: string;
    groupSlug: string;
    groupImage?: string;
    categories: ShopCategoryItem[];
}

export interface ShopProductFilterParams {
    search?: string;
    categoryGroupSlug?: string;
    categorySlug?: string;
    minPrice?: number;
    maxPrice?: number;
    origin?: string;
    certification?: string;
    sortBy?: string;
    pageIndex?: number;
    pageSize?: number;
}
