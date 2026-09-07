import { PaginationParams } from './common';

export interface UoMConversion {
  id: number;
  fromUoMId: number;
  fromUoMName?: string;
  fromUoMCode?: string;

  toUoMId: number;
  toUoMName?: string;
  toUoMCode?: string;

  conversionFactor: number;

  productId: number | null;
  productName?: string | null;
  productCode?: string | null;

  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UoMConversionPayload {
  fromUoMId: number;
  toUoMId: number;
  conversionFactor: number;
  productId: number | null;
  isActive: boolean;
}

export interface UoMConversionQueryParams extends PaginationParams {
  productId?: number;
  isStandard?: boolean;
  isActive?: boolean;
}

export interface ValidUoMOption {
  uoMId: number;
  uoMName: string;
  uoMCode: string;
  conversionFactorToBase: number;
  isBaseUoM: boolean;
  description: string;
}
