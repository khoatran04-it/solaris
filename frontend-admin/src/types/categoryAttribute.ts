import { PaginationParams } from './common';
export interface CategoryAttribute {
  id: number;
  categoryId: number;
  categoryName?: string;
  attributeDefinitionId?: number;
  attributeDefinitionName?: string;
  isRequired: boolean;
}

export interface CategoryAttributePayload {
  categoryId: number;
  attributeDefinitionId?: number;
  isRequired: boolean;
}

export interface CategoryAttributeQueryParams extends PaginationParams {
  categoryId?: string;
  attributeDefinitionId?: string;
}

export interface CategoryAttributeSyncItem {
  attributeDefinitionId: number;
  isRequired: boolean;
}

export interface CategoryAttributeSyncPayload {
  categoryId: number;
  attributes: CategoryAttributeSyncItem[];
}
