export interface CustomerType {
    id: number;
    code: string;
    name: string;
    description?: string;
    createdAt: string;
    updatedAt: string;
}

export interface CustomerTypePayload {
    code: string;
    name: string;
    description?: string;
}