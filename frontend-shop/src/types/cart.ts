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

export interface ShopCartAddPayload {
    variantId: number;
    uoMId: number;
    quantity: number;
}

export interface ShopCartUpdatePayload {
    quantity: number;
}

export interface ShopGuestCartItem {
    variantId: number;
    uoMId: number;
    quantity: number;
}

export interface ShopSyncGuestCartPayload {
    items: ShopGuestCartItem[];
}
