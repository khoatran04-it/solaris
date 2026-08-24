import { PaginationParams } from './common';
export interface AttributeDefinition {
  id: number;
  name: string;
  dataType: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AttributeDefinitionPayload {
  name: string;
  dataType: string;
  isActive: boolean;
}

export interface AttributeDefinitionQueryParams extends PaginationParams {
  isActive?: boolean;
  dataType?: string;
}
