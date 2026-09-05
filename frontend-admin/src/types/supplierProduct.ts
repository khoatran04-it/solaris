import { PaginationParams } from './common';

export interface SupplierProduct {
  id: number;
  supplierSKU?: string;
  lastImportPrice: number;
  minimumOrderQuantity: number;
  leadTimeDays: number;
  isActive: boolean;
  variantId: number;
  supplierId: number;
  purchaseUoMId: number;
  createdAt: string;
  updatedAt: string;

  // Enriched fields from Backend
  variantCode?: string;
  variantName?: string;
  variantSKU?: string;
  variantBarcode?: string;
  variantImage?: string;
  variantImagePath?: string;
  supplierCode?: string;
  supplierName?: string;
  purchaseUoMName?: string;
}

export interface SupplierProductPayload {
  supplierSKU?: string;
  lastImportPrice: number;
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
  createdAt?: string;
  updatedAt?: string;
}
