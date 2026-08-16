
//Supplier Type
export interface SupplierType {
  id: number;
  code: string;
  name: string;
  description: string;
  createdAt: string; // ISO date string
  updatedAt: string; // ISO date string
}

export type SupplierTypePayload = Omit<SupplierType, 'id' | 'createdAt' | 'updatedAt'>;