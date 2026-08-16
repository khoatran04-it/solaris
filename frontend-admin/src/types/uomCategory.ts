export interface UoMCategory {
    id: number;
    code: string;
    name: string;
    baseUoMId: number | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface UoMCategoryPayload {
    code: string;
    name: string;
    baseUoMId?: number | null;
    isActive?: boolean;
}