import { PaginationParams } from './common';
export interface ProductBatch {
  id: number;
  batchCode: string;
  manufactureDate: string;
  expiryDate: string;
  isActive: boolean;
  variantId: number;
  variantName?: string;
  supplierId: number;
  supplierName?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ProductBatchPayload {
  batchCode: string;
  manufactureDate: string;
  expiryDate: string;
  isActive: boolean;
  variantId: number;
  supplierId: number;
}

export interface ProductBatchQueryParams extends PaginationParams {
  isActive?: boolean;
  variantId?: number;
  supplierId?: number;
}
