import { PaginationParams } from './common';

export interface UoMQueryParams extends PaginationParams {
    categoryId?: number;
    isActive?: boolean;
}

export interface UoM {
    id: number;
    code: string;
    name: string;
    categoryId: number;
    categoryName?: string;
    synonyms?: string | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface UoMPayload {
    code: string;
    name: string;
    categoryId: number;
    synonyms?: string | null;
    isActive: boolean;
}