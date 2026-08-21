export interface PagedResult<T> {
    items: T[];
    totalRecords: number;
    totalPages: number;
    currentPage: number;
    pageSize: number;
}

export interface ShopCustomerInfo {
    id: number;
    code: string;
    name: string;
    phoneNumber: string;
    email?: string;
    avatarPath?: string;
    customerTierId?: number;
    customerTierName?: string;
    discountPercent: number;
}

export interface ShopAuthResponse {
    token: string;
    customerInfo: ShopCustomerInfo;
}

export interface ShopAddress {
    id: number;
    receiverName: string;
    phone: string;
    province: string;
    district: string;
    ward: string;
    streetAddress: string;
    fullAddress: string;
    isDefault: boolean;
    latitude: number;
    longitude: number;
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

export interface ShopPromotionBadge {
    id: number;
    name: string;
    slug: string;
    isPercentage: boolean;
    discountValue: number;
    endDate: string;
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

export interface ShopCartItem {
    id: number;
    variantId: number;
    variantName: string;
    variantCode: string;
    productSlug?: string;
    imagePath?: string;
    uoMId: number;
    uoMName: string;
    quantity: number;
    originalPrice: number;
    unitPrice: number;
    discountAmount: number;
    totalPrice: number;
    availableStock: number;
    isOutOfStock: boolean;
    origin?: string;
}

export interface ShopCart {
    cartId: number;
    items: ShopCartItem[];
    totalItems: number;
    subTotal: number;
    totalDiscount: number;
    estimatedTotal: number;
}

export interface ShopOrderItem {
    detailId: number;
    variantId: number;
    variantName: string;
    variantCode: string;
    imagePath?: string;
    uoMId: number;
    uoMName: string;
    quantity: number;
    unitPrice: number;
    discountAmount: number;
    totalPrice: number;
    issuedQuantity: number;
}

export interface ShopOrder {
    id: number;
    orderCode: string;
    orderDate: string;
    status: number;
    statusName: string;
    paymentStatus: number;
    paymentStatusName: string;
    paymentMethod: number;
    paymentMethodName: string;
    subTotal: number;
    discountAmount: number;
    shippingFee: number;
    totalAmount: number;
    receiverName?: string;
    receiverPhone?: string;
    deliveryAddress?: string;
    note?: string;
    cancellationReason?: string;
    items: ShopOrderItem[];
}

export interface ShopReturnItem {
    variantId: number;
    variantName: string;
    variantCode: string;
    batchCode?: string;
    uoMName: string;
    returnedQuantity: number;
    acceptedQuantity: number;
    damagedQuantity: number;
    refundAmount: number;
    rejectReason?: string;
}

export interface ShopReturn {
    id: number;
    returnCode: string;
    orderCode: string;
    returnDate: string;
    status: number;
    statusName: string;
    refundAmount: number;
    reason?: string;
    inspectionNotes?: string;
    details: ShopReturnItem[];
}

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
