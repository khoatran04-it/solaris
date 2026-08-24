import { PaginationParams } from './common';

export interface CustomerTierQueryParams extends PaginationParams {
  names?: string;
  isActive?: boolean;
}

export interface CustomerTier {
  id: number;
  code: string;
  name: string;
  discountPercent: number;
  minSpending: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerTierPayload {
  code: string;
  name: string;
  discountPercent: number;
  minSpending: number;
  isActive: boolean;
}
