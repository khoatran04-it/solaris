import { PaginationParams } from './common';
export interface ProductCategoryGroup {
    id: number;
    code: string;
    name: string;
    description?: string;
    imagePath?: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface ProductCategoryGroupPayload {
    name: string;
    code: string;
    description?: string;
    imagePath?: string;
    isActive?: boolean;
}

export interface ProductCategoryGroupQueryParams extends PaginationParams {
    isActive?: boolean;
}