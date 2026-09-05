import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum InventoryIssueStatus {
  Pending = 1,
  Picking = 2,
  Completed = 3,
  Cancelled = 4,
}

export const InventoryIssueStatusLabels: Record<InventoryIssueStatus, string> = {
  [InventoryIssueStatus.Pending]: 'Chờ xử lý',
  [InventoryIssueStatus.Picking]: 'Đang đóng gói',
  [InventoryIssueStatus.Completed]: 'Đã xuất kho',
  [InventoryIssueStatus.Cancelled]: 'Đã hủy',
};

export const InventoryIssueStatusColors: Record<InventoryIssueStatus, string> = {
  [InventoryIssueStatus.Pending]: 'bg-amber-50 text-amber-700 border-amber-200',
  [InventoryIssueStatus.Picking]: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  [InventoryIssueStatus.Completed]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  [InventoryIssueStatus.Cancelled]: 'bg-rose-50 text-rose-700 border-rose-200',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface InventoryIssueDetail {
  id: number;
  orderDetailId?: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  batchId: number;
  batchCode: string;
  uoMId: number;
  uoMName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface InventoryIssue {
  id: number;
  issueCode: string;

  orderId?: number;
  orderCode?: string;

  warehouseId: number;
  warehouseName: string;

  issuedById: number;
  issuedByName: string;

  issueDate: string;
  status: InventoryIssueStatus;

  receiverName?: string;
  receiverPhone?: string;
  deliveryAddress?: string;

  note?: string;
  cancellationReason?: string;

  createdAt: string;
  updatedAt: string;

  details: InventoryIssueDetail[];
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface InventoryIssueDetailCreatePayload {
  orderDetailId?: number;
  variantId: number;
  batchId: number;
  uoMId: number;
  quantity: number;
  unitPrice: number;
}

export interface InventoryIssueCreatePayload {
  orderId?: number;
  warehouseId: number;
  issuedById?: number;
  issueDate?: string;
  receiverName?: string;
  receiverPhone?: string;
  deliveryAddress?: string;
  note?: string;
  details: InventoryIssueDetailCreatePayload[];
}

export interface SuggestedBatch {
  batchId: number;
  batchCode: string;
  expiryDate?: string;
  quantityAvailable: number;
  quantityReserved: number;
  suggestedPickQuantity: number;
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface InventoryIssueQueryParams extends PaginationParams {
  warehouseId?: number | string;
  status?: number;
  startDate?: string;
  endDate?: string;
}
