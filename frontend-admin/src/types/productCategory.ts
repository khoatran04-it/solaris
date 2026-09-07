import { PaginationParams } from './common';

export interface ProductCategoryQueryParams extends PaginationParams {
  names?: string;
  categoryGroupId?: string;
  isActive?: boolean;
}

export interface ProductCategory {
  id: number;
  code: string;
  name: string;
  description?: string;
  imagePath?: string;
  createdAt: string;
  updatedAt: string;
  categoryGroupId?: number;
  categoryGroupName?: string;
  isActive: boolean;
}

export interface ProductCategoryPayload {
  code: string;
  name: string;
  description?: string;
  imagePath?: string;
  categoryGroupId?: number;
  isActive: boolean;
}
