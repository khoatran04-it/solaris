import { PaginationParams } from './common';
export interface ProductAttribute {
  id: number;
  attributeDefinitionId?: number;
  attributeDefinitionName?: string;
  attributeValue: string;
  displayOrder: number;
  variantId: number;
  variantName?: string;
  createdAt: string;
  updatedAt: string;
  isActive: boolean;
}

export interface ProductAttributePayload {
  attributeDefinitionId?: number;
  attributeValue: string;
  displayOrder: number;
  variantId: number;
  isActive: boolean;
}

export interface ProductAttributeQueryParams extends PaginationParams {
  isActive?: boolean;
  variantId?: number;
}
