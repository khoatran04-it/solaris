export interface ChatSession {
    id: number;
    sessionToken: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    totalMessages: number;
    lastMessage?: string;
}

export interface ChatMessage {
    id: number;
    role: 'user' | 'model' | 'system';
    content: string;
    payloadType: 'none' | 'product_cards' | 'interactive_order' | 'order_success';
    payload?: any;
    createdAt: string;
}

export interface InteractiveOrderItem {
    variantId: number;
    variantCode: string;
    variantName: string;
    slug?: string;
    imagePath?: string;
    uoMId: number;
    uoMName: string;
    quantity: number;
    unitPrice: number;
    discountAmount: number;
    totalPrice: number;
}

export interface InteractiveOrderPayload {
    title: string;
    previousOrderCode?: string;
    items: InteractiveOrderItem[];
    subTotal: number;
    totalDiscount: number;
    shippingFee: number;
    totalAmount: number;
    isFreeShipping: boolean;
    freeShippingThreshold: number;
    suggestedDeliveryAddress?: string;
    suggestedReceiverName?: string;
    suggestedReceiverPhone?: string;
}

export interface AiProductCard {
    id: number;
    variantId: number;
    name: string;
    slug: string;
    imagePath?: string;
    price: number;
    discountedPrice: number;
    uoMName: string;
    origin?: string;
    certification?: string;
    brixLevel?: string;
    isInStock: boolean;
}

export interface ConfirmInteractiveOrderPayload {
    sessionId: number;
    items: InteractiveOrderItem[];
    receiverName?: string;
    receiverPhone?: string;
    deliveryAddress?: string;
    ghnDistrictId?: number;
    ghnWardCode?: string;
    shippingFee: number;
    paymentMethod: number; // 3 = VNPay, 1 = COD
    note?: string;
}

export interface ConfirmInteractiveOrderResponse {
    orderId: number;
    orderCode: string;
    totalAmount: number;
    paymentMethodName: string;
    paymentUrl?: string;
    message?: string;
}
