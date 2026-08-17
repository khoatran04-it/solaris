import { PaginationParams } from './common';

export interface UoMCategoryQueryParams extends PaginationParams {
    isActive?: boolean;
}

export interface UoMCategory {
    id: number;
    code: string;
    name: string;
    baseUoMId: number | null;
    baseUoMName?: string | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface UoMCategoryPayload {
    code: string;
    name: string;
    baseUoMId?: number | null;
    isActive: boolean;
}