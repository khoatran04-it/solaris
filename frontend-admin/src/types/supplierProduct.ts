import { PaginationParams } from './common';
export interface SupplierProduct {
    id: number;
    supplierSku?: string;
    lastImportPrice?: number;
    minimumOrderQuantity: number;
    leadTimeDays: number;
    isActive: boolean;
    variantId: number;
    variantName?: string;
    supplierId: number;
    supplierName?: string;
    purchaseUoMId: number;
    purchaseUoMName?: string;
    createdAt: string;
    updatedAt: string;
}

export interface SupplierProductPayload {
    supplierSku?: string;
    lastImportPrice?: number;
    minimumOrderQuantity: number;
    leadTimeDays: number;
    isActive: boolean;
    variantId: number;
    supplierId: number;
    purchaseUoMId: number;
}

export interface SupplierProductQueryParams extends PaginationParams {
    isActive?: boolean;
    variantId?: number;
    supplierId?: number;
}