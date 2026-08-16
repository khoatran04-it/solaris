export interface CustomerTier {
    id: number;
    code: string;
    name: string;
    discountPercent: number;
    minSpending: number;
    createdAt: string;
    updatedAt: string;
}

export interface CustomerTierPayload {
    code: string;
    name: string;
    discountPercent: number;
    minSpending: number;
}