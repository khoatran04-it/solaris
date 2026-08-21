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

export interface ShopReturnItemPayload {
    variantId: number;
    batchId: number;
    uoMId: number;
    returnedQuantity: number;
    reason?: string;
}

export interface ShopReturnCreatePayload {
    orderCode: string;
    reason?: string;
    items: ShopReturnItemPayload[];
}
