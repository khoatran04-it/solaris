import { PaginationParams } from './common';

export interface CustomerTypeQueryParams extends PaginationParams {
  names?: string;
  isActive?: boolean;
}

export interface CustomerType {
  id: number;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerTypePayload {
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
}
