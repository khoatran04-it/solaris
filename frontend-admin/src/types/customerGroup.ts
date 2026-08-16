export interface CustomerGroup {
    id: number;
    code: string;
    name: string;
    description?: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
}

export interface CustomerGroupPayload {
    code: string;
    name: string;
    description?: string;
    isActive: boolean;
}