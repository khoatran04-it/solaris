import { PaginationParams } from './common';

// 🔥 BỔ SUNG: Type hứng cấu hình thuộc tính động (EAV) từ Backend
export interface ProductAttributeConfig {
  id: number;
  name: string;
  isRequired: boolean;
}

export interface Product {
  id: number;
  code: string;
  name: string;
  description?: string;
  imagePath?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  categoryId?: number;
  categoryName?: string;
  baseUoMId: number;
  baseUoMName?: string;
}

export interface ProductPayload {
  code: string;
  name: string;
  description?: string;
  imagePath?: string;
  isActive: boolean;
  categoryId?: number;
  baseUoMId: number;
}

export interface ProductQueryParams extends PaginationParams {
  categoryId?: string;
  baseUoMId?: string;
  isActive?: boolean;
}
