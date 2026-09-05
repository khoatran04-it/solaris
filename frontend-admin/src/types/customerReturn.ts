import { PaginationParams } from './common';

// =========================================================
// 1. ENUMS & UI LABELS / COLORS
// =========================================================
export enum CustomerReturnStatus {
  Pending = 1,
  Approved = 2,
  Inspecting = 3,
  Completed = 4,
  Rejected = 5,
}

export const CustomerReturnStatusLabels: Record<CustomerReturnStatus, string> = {
  [CustomerReturnStatus.Pending]: 'Chờ tiếp nhận',
  [CustomerReturnStatus.Approved]: 'Đã duyệt',
  [CustomerReturnStatus.Inspecting]: 'Đang xử lý',
  [CustomerReturnStatus.Completed]: 'Đã hoàn tất',
  [CustomerReturnStatus.Rejected]: 'Từ chối trả hàng',
};

export const CustomerReturnStatusColors: Record<CustomerReturnStatus, string> = {
  [CustomerReturnStatus.Pending]: 'bg-amber-50 text-amber-700 border-amber-200',
  [CustomerReturnStatus.Approved]: 'bg-blue-50 text-blue-700 border-blue-200',
  [CustomerReturnStatus.Inspecting]: 'bg-purple-50 text-purple-700 border-purple-200',
  [CustomerReturnStatus.Completed]: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  [CustomerReturnStatus.Rejected]: 'bg-rose-50 text-rose-700 border-rose-200',
};

// =========================================================
// 2. MODELS
// =========================================================
export interface CustomerReturnDetail {
  id: number;
  variantId: number;
  variantName: string;
  variantCode: string;
  batchId: number;
  batchCode: string;
  uoMId: number;
  uoMName: string;
  returnedQuantity: number;
  acceptedQuantity: number;
  damagedQuantity: number;
  unitPrice: number;
  refundAmount: number;
  rejectReason?: string;
}

export interface CustomerReturn {
  id: number;
  returnCode: string;

  orderId: number;
  orderCode: string;

  customerId: number;
  customerName: string;

  warehouseId: number;
  warehouseName: string;

  receivedById?: number;
  receivedByName?: string;

  returnDate: string;
  status: CustomerReturnStatus;

  refundAmount: number;
  reason?: string;
  inspectionNotes?: string;

  createdAt: string;
  updatedAt: string;

  details: CustomerReturnDetail[];
}

// =========================================================
// 3. PAYLOADS
// =========================================================
export interface CustomerReturnDetailCreatePayload {
  variantId: number;
  batchId: number;
  uoMId: number;
  returnedQuantity: number;
  unitPrice?: number;
}

export interface CustomerReturnCreatePayload {
  orderId: number;
  customerId: number;
  warehouseId: number;
  receivedById?: number;
  returnDate?: string;
  reason?: string;
  details: CustomerReturnDetailCreatePayload[];
}

export interface CustomerReturnItemInspectionPayload {
  detailId: number;
  acceptedQuantity: number;
  damagedQuantity: number;
  rejectReason?: string;
}

export interface CustomerReturnInspectionPayload {
  inspectionNotes?: string;
  items: CustomerReturnItemInspectionPayload[];
}

// =========================================================
// 4. QUERY PARAMS
// =========================================================
export interface CustomerReturnQueryParams extends PaginationParams {
  warehouseId?: number | string;
  status?: number;
  startDate?: string;
  endDate?: string;
}
