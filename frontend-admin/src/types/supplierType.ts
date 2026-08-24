import { PaginationParams } from './common';
export interface SupplierTypeQueryParams extends PaginationParams {
  isActive?: boolean;
}
//Supplier Type
export interface SupplierType {
  id: number;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  createdAt: string; // ISO date string
  updatedAt: string; // ISO date string
}

export type SupplierTypePayload = Omit<SupplierType, 'id' | 'createdAt' | 'updatedAt'>;
